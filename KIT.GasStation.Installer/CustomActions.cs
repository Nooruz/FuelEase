using KIT.GasStation.Installer;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using WixToolset.Dtf.WindowsInstaller;
using IoFile = System.IO.File;

namespace WixSharp
{
    /// <summary>
    /// Отложенные (Deferred, Elevated) действия установщика.
    /// Выполняются с правами SYSTEM во время InstallExecuteSequence.
    /// </summary>
    public static class CustomActions
    {
        // Имена папок берём из Program.cs, чтобы держать конфигурацию в одном месте.
        private const string MainAppFolder = Program.MainFolder; // "КИТ-АЗС"

        /// <summary>
        /// Создаёт SQL-логин приложения со случайным сильным паролем
        /// и записывает в папку установленного ПО зашифрованный DPAPI файл credentials.dat.
        /// </summary>
        [CustomAction]
        public static ActionResult InitializeDatabase(Session session)
        {
            try
            {
                var server = Prop(session, "DB_SERVER", ".");
                var database = Prop(session, "DB_NAME", "FuelEaseDb");
                var appLogin = Prop(session, "DB_APP_LOGIN", "KITGasStationApp");
                var appPassword = Prop(session, "DB_APP_PASSWORD", "");
                var installDir = Prop(session, "INSTALLDIR", null);

                if (string.IsNullOrEmpty(appPassword))
                    appPassword = GenerateStrongPassword(24);

                session.Log($"[KIT installer] DB init: server={server}, db={database}, login={appLogin}");

                // К SQL Server подключаемся через Windows-аутентификацию (учётку admin’а, который ставит ПО).
                var adminCs = $"Server={server};Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;";
                using (var cn = new SqlConnection(adminCs))
                {
                    cn.Open();

                    Exec(cn, $"IF DB_ID(N'{Esc(database)}') IS NULL CREATE DATABASE [{database}];");

                    // Создаём/обновляем LOGIN c запретом повторного пароля и сильной политикой.
                    Exec(cn,
                        $"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name=N'{Esc(appLogin)}') " +
                        $"CREATE LOGIN [{appLogin}] WITH PASSWORD=N'{Esc(appPassword)}', CHECK_POLICY=ON, CHECK_EXPIRATION=OFF;");
                    Exec(cn,
                        $"ALTER LOGIN [{appLogin}] WITH PASSWORD=N'{Esc(appPassword)}';");

                    // Переключаемся на целевую БД
                    cn.ChangeDatabase(database);

                    // Создаём USER и выдаём права владельца схемы dbo.
                    Exec(cn,
                        $"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name=N'{Esc(appLogin)}') " +
                        $"CREATE USER [{appLogin}] FOR LOGIN [{appLogin}];");
                    Exec(cn, $"ALTER ROLE db_owner ADD MEMBER [{appLogin}];");
                }

                // Записываем защищённую строку подключения рядом с основным приложением.
                if (!string.IsNullOrEmpty(installDir))
                {
                    var cs = $"Data Source={server};Initial Catalog={database};User ID={appLogin};Password={appPassword};Encrypt=True;TrustServerCertificate=True;";
                    WriteProtected(Path.Combine(installDir, MainAppFolder, "credentials.dat"), cs);
                    session.Log("[KIT installer] credentials.dat записан.");
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("[KIT installer] InitializeDatabase failed: " + ex);
                // Не фейлим установку, чтобы пользователь мог настроить БД вручную.
                // Если хотите жёсткий режим — замените на ActionResult.Failure.
                return ActionResult.Success;
            }
        }

        /// <summary>
        /// Сохраняет лицензионный ключ (LICENSE_KEY) в защищённый DPAPI-файл activation.dat
        /// внутри папки установленного ПО.
        /// </summary>
        [CustomAction]
        public static ActionResult WriteActivation(Session session)
        {
            try
            {
                var key = Prop(session, "LICENSE_KEY", null);
                var installDir = Prop(session, "INSTALLDIR", null);
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(installDir))
                    return ActionResult.Success;

                var path = Path.Combine(installDir, MainAppFolder, "activation.dat");
                WriteProtected(path, key);
                session.Log("[KIT installer] activation.dat записан.");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("[KIT installer] WriteActivation failed: " + ex);
                return ActionResult.Success;
            }
        }

        /// <summary>
        /// Разрешает локальным пользователям (BU) и аутентифицированным пользователям (AU)
        /// запускать и останавливать службы KITWeb/KITWorker через sc.exe sdset.
        /// </summary>
        [CustomAction]
        public static ActionResult GrantServicePermissions(Session session)
        {
            try
            {
                var names = Prop(session, "SERVICE_NAMES", "");
                foreach (var svc in names.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = svc.Trim();
                    if (name.Length == 0) continue;

                    // SDDL:
                    //   (A;;CCLCSWRPWPDTLOCRRC;;;SY)                  — LocalSystem полный доступ к управлению
                    //   (A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)          — Администраторы полный контроль
                    //   (A;;CCLCSWRPWPLOCRRC;;;BU)                    — Builtin\Users: start/stop/query
                    //   (A;;CCLCSWRPWPLOCRRC;;;AU)                    — Authenticated Users: start/stop/query
                    const string sddl =
                        "D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)" +
                        "(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)" +
                        "(A;;CCLCSWRPWPLOCRRC;;;BU)" +
                        "(A;;CCLCSWRPWPLOCRRC;;;AU)";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"sdset \"{name}\" \"{sddl}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) continue;
                        var stdout = proc.StandardOutput.ReadToEnd();
                        var stderr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit(15000);
                        session.Log($"[KIT installer] sc sdset {name} exit={proc.ExitCode} out={stdout.Trim()} err={stderr.Trim()}");
                    }
                }
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("[KIT installer] GrantServicePermissions failed: " + ex);
                return ActionResult.Success;
            }
        }

        // ---------- helpers ----------

        private static string Prop(Session s, string name, string fallback)
        {
            try
            {
                // В Deferred CA свойства доступны только если переданы через CustomActionData.
                // WixSharp сам формирует CustomActionData из UsesProperties, поэтому читаем CustomActionData.
                var v = s.CustomActionData.ContainsKey(name) ? s.CustomActionData[name] : null;
                if (string.IsNullOrEmpty(v))
                    v = s[name];
                return string.IsNullOrEmpty(v) ? fallback : v;
            }
            catch
            {
                return fallback;
            }
        }

        private static void Exec(SqlConnection cn, string sql)
        {
            using (var cmd = new SqlCommand(sql, cn) { CommandTimeout = 30 })
                cmd.ExecuteNonQuery();
        }

        private static string Esc(string value) =>
            (value ?? string.Empty).Replace("'", "''").Replace("]", "]]");

        private static string GenerateStrongPassword(int length)
        {
            // Алфавит без неоднозначных символов.
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^*_+";
            using (var rnd = new RNGCryptoServiceProvider())
            {
                var buf = new byte[length];
                rnd.GetBytes(buf);
                var sb = new StringBuilder(length);
                for (int i = 0; i < length; i++)
                    sb.Append(chars[buf[i] % chars.Length]);
                return sb.ToString();
            }
        }

        private static void WriteProtected(string path, string plainText)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
            IoFile.WriteAllBytes(path, encrypted);
        }
    }
}
