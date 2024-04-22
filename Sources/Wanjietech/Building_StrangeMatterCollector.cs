using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.Sound;
using Verse;
using System.Globalization;

namespace Wanjietech {
    internal class Building_StrangeMatterCollector : Building {
        public const string thingName = "StrangeMatter";
        // 每日生成的数量：每天会为numContained添加的量
        public const float numGeneratedRerday = 10.0f;
        // 收获阈值：当numContained大于收获阈值时才可自动收获
        private const int HaulingThreshold = 30;
        // 最大存储量：numContained的存储上限
        private const int MaxCapacity = 60;
        // 内容物数量：当前存储的数量
        public int numContained = 0;
        // 正在收集数量：当达到1时numContained++
        public float numCollecting = .0f;
        // 启用卸载：启动卸载时，小人才会主动去收获
        public bool unloadingEnabled = true;
        // 暂时没啥用
        private bool initalized;


        private CompPowerTrader powerComp;
        private Wanjietech.CompLoadStrMatCon loadStrMatCon;
        // 是一个用于管理声音效果的类。具体来说，它负责播放、停止、淡入和淡出游戏中的各种声音，例如环境音效、音乐、角色对话等。
        private Sustainer workingSustainer;

        public CompPowerTrader Power => powerComp ?? (powerComp = GetComp<CompPowerTrader>());
        public Wanjietech.CompLoadStrMatCon StrMatCon => loadStrMatCon ?? (loadStrMatCon = GetComp<CompLoadStrMatCon>());

        public ThingDef thingDef => DefDatabase<ThingDef>.GetNamed(thingName);

        public bool ReadyForHauling => numContained >= HaulingThreshold;

        // 每日实际产量
        private float ProductionPerDay {
            get {
                if (Power.PowerOn && StrMatCon.HasFuel && numContained<MaxCapacity) {
                    return numGeneratedRerday;
                }
                return 0f;
            }
        }

        // 当建筑被放置
        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad) {
                Initialize();
            }
        }

        // 当建筑被拆除
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish) {
            base.DeSpawn(mode);
            initalized = false;
            workingSustainer?.End();
            workingSustainer = null;
        }

        private void Initialize() {
            if (initalized) {
                return;
            }
            initalized = true;
        }

        // 每一帧都会调用
        protected override void DrawAt(Vector3 drawLoc, bool flip = false) {
            base.DrawAt(drawLoc, flip);
            if (!initalized) {
                Initialize();
            }
        }

        public override void Tick() {
            base.Tick();
            if (this.IsHashIntervalTick(250)) {
                if (numContained<MaxCapacity && StrMatCon.HasFuel) {
                    numCollecting += ProductionPerDay / 60000f * 250f;
                }
                if (numCollecting >= 1) {
                    numContained++;
                    numCollecting--;
                    StrMatCon.ConsumeFuel(1);
                }
            }
            if (IsWorking()) {
                if (workingSustainer == null) {
                    workingSustainer = SoundDefOf.BioferriteHarvester_Ambient.TrySpawnSustainer(SoundInfo.InMap(this));
                }

                workingSustainer.Maintain();
            } else {
                workingSustainer?.End();
                workingSustainer = null;
            }
        }

        public override bool IsWorking() {
            return ProductionPerDay != 0f;
        }

        // 获取检查字符串（在游戏左下角检查面板中显示）
        public override string GetInspectString() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0) {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("BuildingStrangeMatterCollectorContained".Translate(thingDef.label));
            stringBuilder.Append($": {numContained} / {MaxCapacity}\n");
            stringBuilder.Append($"Collecting: {numCollecting*100:F2}% (+{ProductionPerDay*100:F0}% ");
            stringBuilder.Append(string.Format("{0})", "BuildingStrangeMatterCollectorPerDay".Translate()));

            return stringBuilder.ToString();
        }

        public Thing TakeOutThing() {
            int num = numContained;
            if (num == 0) {
                return null;
            }

            numContained -= num;
            Thing thing = ThingMaker.MakeThing(thingDef);
            thing.stackCount = num;
            return thing;
        }
        // 添加游戏按钮
        public override IEnumerable<Gizmo> GetGizmos() {
            foreach (Gizmo gizmo in base.GetGizmos()) {
                yield return gizmo;
            }
            // 切换开关
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.defaultLabel = "BuildingStrangeMatterCollectorToggleUnloading".Translate(thingDef.label);
            command_Toggle.defaultDesc = "BuildingStrangeMatterCollectorToggleUnloadingDesc".Translate(thingDef.label);
            command_Toggle.isActive = () => unloadingEnabled;
            command_Toggle.toggleAction = delegate {
                unloadingEnabled = !unloadingEnabled;
            };
            command_Toggle.activateSound = SoundDefOf.Tick_Tiny;
            command_Toggle.icon = ContentFinder<Texture2D>.Get("UI/Commands/BioferriteUnloading");
            yield return command_Toggle;
            // 弹出
            if (numContained >= 1) {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "BuildingStrangeMatterCollectorEjectContents".Translate();
                command_Action.defaultDesc = "BuildingStrangeMatterCollectorEjectContentsDesc".Translate(Find.ActiveLanguageWorker.Pluralize(thingDef.label));
                command_Action.action = delegate {
                    EjectContents();
                };
                command_Action.Disabled = numContained == 0;
                command_Action.activateSound = SoundDefOf.Tick_Tiny;
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/EjectBioferrite");
                yield return command_Action;
            }
            // 添加
            if (DebugSettings.ShowDevGizmos) {
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Add +1 bioferrite";
                command_Action2.action = delegate {
                    numContained = Mathf.Min(numContained + 1, MaxCapacity);
                };
                yield return command_Action2;
            }
        }
        // 弹出
        private void EjectContents() {
            Thing thing = TakeOutThing();
            if (thing != null) {
                GenPlace.TryPlaceThing(thing, base.Position, base.Map, ThingPlaceMode.Near);
            }
        }

        public override void Notify_DefsHotReloaded() {
            base.Notify_DefsHotReloaded();
            RebuildCables();
        }

        private void RebuildCables() {
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref numContained, "numContained", 0);
            Scribe_Values.Look(ref numCollecting, "numCollecting", 0f);
            Scribe_Values.Look(ref unloadingEnabled, "unloadingEnabled", defaultValue: false);
        }

        public static string ToTitleCase(string str) {
            if (string.IsNullOrEmpty(str))
                return str;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(str.ToLower());
        }
    }
}
