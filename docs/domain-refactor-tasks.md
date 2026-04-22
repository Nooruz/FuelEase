# Domain refactor task list

## Goal
Bring `KIT.GasStation.Domain` to an enterprise-ready state for large gas station clients.

## Priority P0
- Remove `INotifyPropertyChanged` from domain entities.
- Remove generic `Update(DomainObject updatedItem)` pattern.
- Remove plain-text password storage and comparisons.
- Introduce `DomainException`.
- Move UI/presentation-only fields and formatted strings out of domain.

## Priority P1
- Introduce clean base abstractions: `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`.
- Add value objects: `Money`, `Volume`, `Percentage`, `DateRange`.
- Replace public setters in key entities with domain methods.
- Add invariants to `Shift`, `FuelSale`, `Tank`, `Fuel`, `Discount`.
- Split persisted configuration and runtime state in `Nozzle`.

## Priority P2
- Extract fuel price history into a separate model.
- Rework `FuelSale` into a stronger aggregate root.
- Rework `Shift` into an aggregate root with explicit open/close flow.
- Add audit fields (`CreatedBy`, `UpdatedBy`, `DeletedBy`, `ClosedBy`).
- Add optimistic concurrency/versioning.
- Add domain events for critical business operations.

## Suggested sequence
1. Foundation and security
2. Rich behavior in aggregates
3. Price history and nozzle runtime split
4. Audit, concurrency, and events
5. Unit tests for domain invariants

## Notes
This branch intentionally starts with foundation changes and planning artifacts first. The existing domain layer currently mixes domain, EF, and WPF concerns, so the refactor should proceed in controlled steps.
