using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Wanjietech {
    internal class WorkGiver_TakeStrangeMatterOutOfCollector : WorkGiver_Scanner {
        public override ThingRequest PotentialWorkThingRequest {
            get {
                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count == 1) {
                    return ThingRequest.ForDef(def.fixedBillGiverDefs[0]);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
            }
        }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false) {
            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
            if (!(t is Wanjietech.Building_StrangeMatterCollector building_Generator) || !building_Generator.unloadingEnabled || !building_Generator.ReadyForHauling) {
                return false;
            }

            if (t.IsBurning()) {
                return false;
            }

            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced)) {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            JobDef takeThingOutOfGenerator = DefDatabase<JobDef>.GetNamed("TakeStrangeMatterOutOfCollector");
            return JobMaker.MakeJob(takeThingOutOfGenerator, t);
        }
    }
}
