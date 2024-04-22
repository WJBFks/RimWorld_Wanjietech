using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Wanjietech {
    internal class Toils_LoadStrMatCon {
        public static Toil FinalizeLoadStrMatCon(TargetIndex refuelableInd, TargetIndex fuelInd) {
            Toil toil = ToilMaker.MakeToil("FinalizeLoadStrMatCon");
            toil.initAction = delegate {
                Job curJob = toil.actor.CurJob;
                Thing thing = curJob.GetTarget(refuelableInd).Thing;
                if (toil.actor.CurJob.placedThings.NullOrEmpty()) {
                    thing.TryGetComp<Wanjietech.CompLoadStrMatCon>().Refuel(new List<Thing> { curJob.GetTarget(fuelInd).Thing });
                } else {
                    thing.TryGetComp<Wanjietech.CompLoadStrMatCon>().Refuel(toil.actor.CurJob.placedThings.Select((ThingCountClass p) => p.thing).ToList());
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
