using System;
using System.Diagnostics.CodeAnalysis; // добавить этот using
using Content.Shared._Forge.Weapons.Longsword.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._Forge.Weapons.Longsword.Systems;

public sealed class GunBatteryAmmoSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunBatteryAmmoComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<GunBatteryAmmoComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private bool TryGetBattery(EntityUid uid, out EntityUid batteryUid, [NotNullWhen(true)] out BatteryComponent? battery)
    {
        batteryUid = default;
        battery = null;

        if (!TryComp<PowerCellSlotComponent>(uid, out var slotComp))
            return false;

        if (!_itemSlots.TryGetSlot(uid, slotComp.CellSlotId, out var slot) || slot.Item is not { } item)
            return false;

        if (!TryComp(item, out battery))
            return false;

        batteryUid = item;
        return true;
    }

    private void OnShotAttempted(Entity<GunBatteryAmmoComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryGetBattery(ent.Owner, out _, out var battery) || battery.CurrentCharge < ent.Comp.FireCost)
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString(ent.Comp.NoChargePopup), ent.Owner, args.User);
        }
    }

    private void OnAmmoShot(EntityUid uid, GunBatteryAmmoComponent component, AmmoShotEvent args)
    {
        var count = Math.Max(args.FiredProjectiles.Count, 1);
        var cost = component.FireCost * count;

        if (TryGetBattery(uid, out var batteryUid, out _))
        {
            _battery.TryUseCharge(batteryUid, cost);
        }
    }
}