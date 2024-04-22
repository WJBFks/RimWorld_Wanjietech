using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Wanjietech {
    internal class CompProperties_LoadStrMatCon : CompProperties {
        // 最大容量
        public int capacity = 50;
        // 自动填充阈值
        public int autoRefuelThreshold = 20;
        // 燃料筛选器
        public ThingFilter fuelFilter;
        // 允许自动填充燃料
        public bool initialAllowAutoRefuel = true;
        // 显示自动填充燃料按钮
        public bool showAllowAutoRefuelToggle = true;
        // 绘制燃料叠加图
        public bool drawOutOfFuelOverlay = true;
        // 在地图中绘制燃油测量仪
        public bool drawFuelGaugeInMap = false;

        [MustTranslate]
        public string fuelLabel;

        [MustTranslate]
        public string fuelGizmoLabel;

        [MustTranslate]
        public string outOfFuelMessage;

        public string fuelIconPath;

        public bool externalTicking;

        private Texture2D fuelIcon;

        public string FuelLabel {
            get {
                if (fuelLabel.NullOrEmpty()) {
                    return "Fuel".TranslateSimple();
                }

                return fuelLabel;
            }
        }

        public string FuelGizmoLabel {
            get {
                if (fuelGizmoLabel.NullOrEmpty()) {
                    return "Fuel".TranslateSimple();
                }

                return fuelGizmoLabel;
            }
        }

        public Texture2D FuelIcon {
            get {
                if (fuelIcon == null) {
                    if (!fuelIconPath.NullOrEmpty()) {
                        fuelIcon = ContentFinder<Texture2D>.Get(fuelIconPath);
                    } else {
                        ThingDef thingDef = ((fuelFilter.AnyAllowedDef == null) ? ThingDefOf.Chemfuel : fuelFilter.AnyAllowedDef);
                        fuelIcon = thingDef.uiIcon;
                    }
                }

                return fuelIcon;
            }
        }

        public CompProperties_LoadStrMatCon() {
            compClass = typeof(Wanjietech.CompLoadStrMatCon);
        }

        public override void ResolveReferences(ThingDef parentDef) {
            base.ResolveReferences(parentDef);
            fuelFilter.ResolveReferences();
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef) {
            foreach (string item in base.ConfigErrors(parentDef)) {
                yield return item;
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req) {
            foreach (StatDrawEntry item in base.SpecialDisplayStats(req)) {
                yield return item;
            }

            if (((ThingDef)req.Def).building.IsTurret) {
                yield return new StatDrawEntry(StatCategoryDefOf.Building, "ShotsBeforeRearm".Translate(), ((int)capacity).ToString(), "ShotsBeforeRearmExplanation".Translate(), 3171);
            }
        }
    }
}
