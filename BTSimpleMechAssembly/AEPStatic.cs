using AccessExtension;
using BattleTech;
using BattleTech.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BTSimpleMechAssembly
{
    static class AEPStatic
    {
        [MethodCall(typeof(SimGameState), "GetItemStatID", new Type[] { typeof(string), typeof(string) })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetItemStatID(this SimGameState s, string id, string type)
        {
            throw new NotImplementedException();
        }

        [MethodCall(typeof(SimGameState), "RemoveItemStat", new Type[] { typeof(string), typeof(string), typeof(bool) })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RemoveItemStat(this SimGameState s, string id, string type, bool b)
        {
            throw new NotImplementedException();
        }

        [PropertySet(typeof(Contract), "SalvagedChassis")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetSalvagedChassis(this Contract c, List<SalvageDef> l)
        {
            throw new NotImplementedException();
        }
        [PropertySet(typeof(Contract), "LostMechs")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetLostMechs(this Contract c, List<MechDef> l)
        {
            throw new NotImplementedException();
        }
        [PropertySet(typeof(Contract), "SalvageResults")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetSalvageResults(this Contract c, List<SalvageDef> l)
        {
            throw new NotImplementedException();
        }
        [PropertySet(typeof(Contract), "FinalSalvageCount")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetFinalSalvageCount(this Contract c, int l)
        {
            throw new NotImplementedException();
        }
        [PropertySet(typeof(Contract), "FinalPrioritySalvageCount")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetFinalPrioritySalvageCount(this Contract c, int l)
        {
            throw new NotImplementedException();
        }

        [MethodCall(typeof(Contract), "CreateAndAddMechPart")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CreateAndAddMechPart(this Contract c, SimGameConstants co, MechDef d, int num, List<SalvageDef> l)
        {
            throw new NotImplementedException();
        }
        [MethodCall(typeof(Contract), "AddMechComponentToSalvage")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddMechComponentToSalvage(this Contract c, List<SalvageDef> l, MechComponentDef d, ComponentDamageLevel dl, bool b, SimGameConstants co, NetworkRandom r, bool b2)
        {
            throw new NotImplementedException();
        }
    }
}
