using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Wanjietech
{
    public class Building_Recombiner : Building_WorkTable, IThingHolder {
        public ThingOwner innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren) {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() {
            return innerContainer;
        }
    }
}
