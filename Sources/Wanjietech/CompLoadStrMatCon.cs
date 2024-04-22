using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Wanjietech {
    internal class CompLoadStrMatCon : ThingComp, IThingGlower {
        private int fuel;

        public bool allowAutoRefuel = true;

        private CompFlickable flickComp;

        public const string RefueledSignal = "Refueled";

        public const string RanOutOfFuelSignal = "RanOutOfFuel";

        private static readonly Vector2 FuelBarSize = new Vector2(1f, 0.2f);

        private static readonly Material FuelBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.6f, 0.56f, 0.13f));

        private static readonly Material FuelBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));
        // 容量
        public int Capacity {
            get {
                return Props.capacity;
            }
        }
        public float FuelPercentOfMax => fuel / Capacity;
        public Wanjietech.CompProperties_LoadStrMatCon Props => (Wanjietech.CompProperties_LoadStrMatCon)props;

       // 是否满燃料
        public bool IsFull => fuel >= Capacity;
        // 是否有足够的燃料
        public bool HasFuel => fuel > 0;
        // 自动加油阈值
        public int AutoRefuelThreshold => Props.autoRefuelThreshold;

        // 现在是否应该自动加油
        public bool ShouldAutoRefuelNow {
            get {
                if (fuel< AutoRefuelThreshold) {
                    return ShouldAutoRefuelNowIgnoringFuelPct;
                }

                return false;
            }
        }
        // 应否现在自动加油忽略燃油百分比
        public bool ShouldAutoRefuelNowIgnoringFuelPct {
            get {
                if (!parent.IsBurning() && (flickComp == null || flickComp.SwitchIsOn) && parent.Map.designationManager.DesignationOn(parent, DesignationDefOf.Flick) == null) {
                    return parent.Map.designationManager.DesignationOn(parent, DesignationDefOf.Deconstruct) == null;
                }

                return false;
            }
        }

        public bool ShouldBeLitNow() {
            return HasFuel;
        }

        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            allowAutoRefuel = Props.initialAllowAutoRefuel;
            fuel = 0;
            flickComp = parent.GetComp<CompFlickable>();
        }

        // 保存数据
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Values.Look(ref fuel, "fuel", 0);
            Scribe_Values.Look(ref allowAutoRefuel, "allowAutoRefuel", defaultValue: false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && !Props.showAllowAutoRefuelToggle) {
                allowAutoRefuel = Props.initialAllowAutoRefuel;
            }
        }

        public override void PostDraw() {
            base.PostDraw();
            if (!allowAutoRefuel) {
                parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.ForbiddenRefuel);
            } else if (!HasFuel && Props.drawOutOfFuelOverlay) {
                parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.OutOfFuel);
            }
            // 在地图中绘制燃油测量仪
            if (Props.drawFuelGaugeInMap) {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = parent.DrawPos + Vector3.up * 0.1f;
                r.size = FuelBarSize;
                r.fillPercent = FuelPercentOfMax;
                r.filledMat = FuelBarFilledMat;
                r.unfilledMat = FuelBarUnfilledMat;
                r.margin = 0.15f;
                Rot4 rotation = parent.Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                r.rotation = rotation;
                GenDraw.DrawFillableBar(r);
            }
        }
        // 销毁后
        public override void PostDestroy(DestroyMode mode, Map previousMap) {
            base.PostDestroy(mode, previousMap);
            //if ((!Props.fuelIsMortarBarrel || !Find.Storyteller.difficulty.classicMortars) && mode != 0 && previousMap != null && Props.fuelFilter.AllowedDefCount == 1 && Props.initialFuelPercent == 0f) {
            //    ThingDef thingDef = Props.fuelFilter.AllowedThingDefs.First();
            //    int num = GenMath.RoundRandom(1f * fuel);
            //    while (num > 0) {
            //        Thing thing = ThingMaker.MakeThing(thingDef);
            //        thing.stackCount = Mathf.Min(num, thingDef.stackLimit);
            //        num -= thing.stackCount;
            //        GenPlace.TryPlaceThing(thing, parent.Position, previousMap, ThingPlaceMode.Near);
            //    }
            //}
        }
        // 组件检查字符串显示
        public override string CompInspectStringExtra() {
            //if (Props.fuelIsMortarBarrel && Find.Storyteller.difficulty.classicMortars) {
            //    return string.Empty;
            //}

            string text = Props.FuelLabel + ": " + fuel + " / " + Capacity;

            if (!HasFuel && !Props.outOfFuelMessage.NullOrEmpty()) {
                string arg = ((parent.def.building != null && parent.def.building.IsTurret) ? ("CannotShoot".Translate() + ": " + Props.outOfFuelMessage).Resolve() : Props.outOfFuelMessage);
                text += $"\n{arg} ({GetFuelCountToFullyRefuel()}x {Props.fuelFilter.AnyAllowedDef.label})";
            }

            return text;
        }
        // 特殊显示统计
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats() {
            if (parent.def.building != null && parent.def.building.IsTurret) {
                TaggedString taggedString = "RearmCostExplanation".Translate();
                taggedString += ".";
                yield return new StatDrawEntry(StatCategoryDefOf.Building, "RearmCost".Translate(), GenLabel.ThingLabel(Props.fuelFilter.AnyAllowedDef, null, GetFuelCountToFullyRefuel()).CapitalizeFirst(), taggedString, 3171);
            }
        }
        // 加燃料
        public void Refuel(List<Thing> fuelThings) {
            int num = GetFuelCountToFullyRefuel();
            while (num > 0 && fuelThings.Count > 0) {
                Thing thing = fuelThings.Pop();
                int num2 = Mathf.Min(num, thing.stackCount);
                Refuel(num2);
                thing.SplitOff(num2).Destroy();
                num -= num2;
            }
        }
        // 加燃料
        public void Refuel(int amount) {
            fuel += amount;
            if (fuel > Capacity) {
                fuel = Capacity;
            }
            parent.BroadcastCompSignal("Refueled");
        }
        public void ConsumeFuel(int amount) {
            fuel -= amount;
            if (fuel <= 0) {
                fuel = 0;
                parent.BroadcastCompSignal("RanOutOfFuel");
            }
        }

        public int GetFuelCountToFullyRefuel() {
            return Capacity - fuel;
        }

        // 按钮
        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            if (Props.showAllowAutoRefuelToggle) {
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.defaultLabel = "CommandToggleAllowAutoRefuel".Translate();
                command_Toggle.defaultDesc = "CommandToggleAllowAutoRefuelDesc".Translate();
                command_Toggle.hotKey = KeyBindingDefOf.Command_ItemForbid;
                command_Toggle.icon = (allowAutoRefuel ? TexCommand.ForbidOff : TexCommand.ForbidOn);
                command_Toggle.isActive = () => allowAutoRefuel;
                command_Toggle.toggleAction = delegate {
                    allowAutoRefuel = !allowAutoRefuel;
                };
                yield return command_Toggle;
            }

            if (DebugSettings.ShowDevGizmos) {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Set fuel to 0";
                command_Action.action = delegate {
                    fuel = 0;
                    parent.BroadcastCompSignal("Refueled");
                };
                yield return command_Action;
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Fuel -20%";
                command_Action2.action = delegate {
                    ConsumeFuel((int)(Capacity * 0.2));
                };
                yield return command_Action2;
                Command_Action command_Action3 = new Command_Action();
                command_Action3.defaultLabel = "DEV: Set fuel to max";
                command_Action3.action = delegate {
                    fuel = Props.capacity;
                    parent.BroadcastCompSignal("Refueled");
                };
                yield return command_Action3;
            }
        }
    }
}
