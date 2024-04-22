using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Wanjietech {
    internal class WorkGiver_LoadStrMatCon : WorkGiver_Scanner {
        public override ThingRequest PotentialWorkThingRequest {
            get {
                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count == 1) {
                    return ThingRequest.ForDef(def.fixedBillGiverDefs[0]);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
            }
        }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public virtual JobDef JobStandard => JobDefOf.Refuel;

        public virtual JobDef JobAtomic => JobDefOf.RefuelAtomic;

        public virtual bool CanRefuelThing(Thing t) {
            return !(t is Building_Turret);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
            if (CanRefuelThing(t)) {
                return Utility_WorkGiverLoadStrMatCon.CanRefuel(pawn, t, forced);
            }

            return false;
        }
        
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            return Utility_WorkGiverLoadStrMatCon.RefuelJob(pawn, t, forced, JobStandard, JobAtomic);
        }
    }
}
