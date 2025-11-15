namespace KIT.GasStation.Mobile.HostBuilders
{
    public static class AddPagesHostBuilderExtensions
    {
        public static MauiAppBuilder AddPages(this MauiAppBuilder builder)
        {
            builder.Services.AddTransient<MainPage>();
            //builder.Services.AddTransient<DetailsPage>();
            // сюда все остальные страницы

            return builder;
        }
    }
}
