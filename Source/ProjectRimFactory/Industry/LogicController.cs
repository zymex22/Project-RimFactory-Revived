using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;


namespace ProjectRimFactory.Industry
{
    public class DynamicSlotGroup
    {
        public IEnumerable<Thing> HeldThings => slotGroup?.HeldThings ?? this.heldThings;
        private SlotGroup slotGroup = null;
        private IEnumerable<Thing> heldThings = null;

        public DynamicSlotGroup(IEnumerable<Thing> things)
        {
            heldThings = things;
        }

        public DynamicSlotGroup(SlotGroup slot)
        {
            slotGroup = slot;
        }
    }

    public interface ILogicObjeckt
    {
        public int GetValue(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2);

        public int Value { set; }

        public string Name { get; set; }
    }

    public enum EnumDynamicSlotGroupID
    {
        NA,
        Group_1,
        Group_2,
        Both
    }

    public interface IDynamicSlotGroup
    {
        public bool UsesDynamicSlotGroup { get; }

        public EnumDynamicSlotGroupID dynamicSlot { get; set; }
    }


    //Base Class
    abstract class ValueRefrence : ILogicObjeckt, IDynamicSlotGroup, IExposable
    {
        abstract public int Value { set; }

        abstract public string Name { get; set; }

        abstract public bool Visible { get; }

        virtual public bool UsesDynamicSlotGroup { get => dynamicSlot != EnumDynamicSlotGroupID.NA; }
        public abstract EnumDynamicSlotGroupID dynamicSlot { get; set; }

        public abstract void ExposeData();

        abstract public int GetValue(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2);
    }

    /// <summary>
    /// ValueRefrence_Fixed Is a Possible Object for the Leaf_Logic Constructor
    /// This one is Used for Fixed Values that the user can Enter
    /// </summary>
    class ValueRefrence_Fixed : ValueRefrence
    {
        private int pvalue;

        private string name;
        public override int Value { set => pvalue = value; }
        public override string Name { get => name; set => name = value; }

        private bool visible = true;
        public override bool Visible => visible;

        public override EnumDynamicSlotGroupID dynamicSlot { get => EnumDynamicSlotGroupID.NA; set => throw new NotImplementedException(); }

        public ValueRefrence_Fixed(int initalVal = 0, string Name = "ValueRefrence_Fixed", bool vis = true)
        {
            visible = vis;
            pvalue = initalVal;
            name = Name;
        }

        public ValueRefrence_Fixed()
        {

        }

        public override int GetValue(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            return pvalue;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref pvalue, "pvalue");
            Scribe_Values.Look(ref visible, "visible");
        }
    }

    /// <summary>
    /// ValueRefrence_Fixed Is a Possible Object for the Leaf_Logic Constructor
    /// This one is Used for Refrencing Logic Signals
    /// </summary>
    class ValueRefrence_Signal : ValueRefrence
    {
        /// <summary>
        /// The Idea is that if this is public then i dont need a new objeck if the user wants to change the Signal
        /// </summary>
        public LogicSignal logicSignal;

        public override string Name { get => logicSignal.Name; set => throw new NotImplementedException(); }

        public override int Value { set => throw new NotImplementedException(); }

        private bool visible = true;
        public override bool Visible => visible;

        public override EnumDynamicSlotGroupID dynamicSlot { get => logicSignal.dynamicSlot; set => throw new NotImplementedException(); }

        public ValueRefrence_Signal(LogicSignal signal)
        {
            logicSignal = signal;
        }

        public override int GetValue(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            return logicSignal.GetValue(DynamicSlot_1, DynamicSlot_2);
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref logicSignal, "logicSignal");
            Scribe_Values.Look(ref visible, "visible");
        }
    }

    /// <summary>
    /// ValueRefrence_Fixed Is a Possible Object for the Leaf_Logic Constructor
    /// This one is Used for Comparing against Thing Counts within a predefind zone
    /// </summary>
    class ValueRefrence_ThingCount : ValueRefrence
    {
        public override int Value
        {
            set
            {
                throw new NotImplementedException();
            }
        }

        private string name;

        public override string Name { get => name; set => name = value; }

        //Holds the filter
        public ThingFilter filter;

        public StorageLocation storage;

        private Map map;

        private bool visible = true;
        public override bool Visible => visible;

        public override EnumDynamicSlotGroupID dynamicSlot { get => storage.dynamicSlot; set => storage.dynamicSlot = value; }

        public ValueRefrence_ThingCount(ThingFilter thingFilter, StorageLocation storageLocation, Map thismap, string Name = "ValueRefrence_ThingCount")
        {
            filter = thingFilter;
            storage = storageLocation;
            map = thismap;
            name = Name;
        }

        public override int GetValue(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            return storage.GetItemCount(filter, map, DynamicSlot_1, DynamicSlot_2);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref visible, "visible");
            Scribe_References.Look(ref map, "map");
            Scribe_Deep.Look(ref filter, "filter");
            Scribe_Deep.Look(ref storage, "storage");
        }
    }
    class StorageLocation : IDynamicSlotGroup, IExposable
    {
        public SlotGroup SlotGroup = null;

        private EnumDynamicSlotGroupID privatedynamicSlot = EnumDynamicSlotGroupID.NA;

        public bool UsesDynamicSlotGroup
        {
            get
            {
                return dynamicSlot != EnumDynamicSlotGroupID.NA;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public EnumDynamicSlotGroupID dynamicSlot { get => privatedynamicSlot; set => privatedynamicSlot = value; }

        public string GetLocationName()
        {
            if (SlotGroup != null) return SlotGroup.parent.SlotYielderLabel();
            if (UsesDynamicSlotGroup)
            {
                if (privatedynamicSlot == EnumDynamicSlotGroupID.Group_1)
                {
                    return "PRF_LogicController_DynamicStorage1".Translate();
                }
                else
                {
                    return "PRF_LogicController_DynamicStorage2".Translate();
                }
            }
            return "PRF_LogicController_EntireMap".Translate();
        }


        public int GetItemCount(ThingFilter filter, Map map, DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            int returnval = 0;
            switch (dynamicSlot)
            {
                case EnumDynamicSlotGroupID.NA:
                    {
                        if (SlotGroup != null)
                        {
                            returnval = SlotGroup.HeldThings.Where(t => filter.Allows(t)).Select(n => n.stackCount).Sum();
                        }
                        else
                        {
                            foreach (ThingDef thing in filter.AllowedThingDefs)
                            {
                                returnval += map.resourceCounter.GetCount(thing);
                            }
                        }
                        return returnval;
                    }
                case EnumDynamicSlotGroupID.Group_1:
                    if (DynamicSlot_1 == null)
                    {
                        Log.Message("PRF - ERROR DynamicSlot_1 == null");
                        return -1;
                    }
                    int count = DynamicSlot_1.HeldThings.Where(t => t != null && filter.Allows(t)).Select(n => n.stackCount).Sum();
                    return count;
                case EnumDynamicSlotGroupID.Group_2:
                    if (DynamicSlot_2 == null)
                    {
                        Log.Message("PRF - ERROR DynamicSlot_2 == null");
                        return -1;
                    }
                    return DynamicSlot_2.HeldThings.Where(t => t != null && filter.Allows(t)).Select(n => n.stackCount).Sum();
                default:
                    {
                        Log.Error("PRF Logic-Controller Invalide DynamicSlot: " + dynamicSlot);
                        return returnval;
                    }

            }




        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref privatedynamicSlot, "privatedynamicSlot");
            Scribe_Deep.Look(ref SlotGroup, "SlotGroup");
        }
    }




    /// <summary>
    /// Logic Operators for Leaf_Logic
    ///  == != < > <= >=
    /// </summary>
    public enum EnumCompareOperator
    {
        Equal,
        NotEqual,
        Greater,
        Smaller,
        GreaterEqual,
        SmallerEqual
    }

    /// <summary>
    /// This is is What's behin 'A' and 'B' in: A AND B
    /// 
    /// </summary>
    public class Leaf_Logic : IDynamicSlotGroup, IExposable
    {
        private string name = "PRF_LogicController_Leaf_Logic_Default_Name".Translate();
        public string Name { get => name; set => name = value; }

        private ILogicObjeckt value1;
        private ILogicObjeckt value2;
        private EnumCompareOperator op;

        private bool visible = true;
        public bool Visible => visible;

        public ILogicObjeckt Value1 { get => value1; set => value1 = value; }
        public ILogicObjeckt Value2 { get => value2; set => value2 = value; }
        public EnumCompareOperator Operator { get => op; set => op = value; }

        public bool UsesDynamicSlotGroup => ((IDynamicSlotGroup)value1).UsesDynamicSlotGroup || ((IDynamicSlotGroup)value2).UsesDynamicSlotGroup;

        public EnumDynamicSlotGroupID dynamicSlot
        {
            get
            {
                EnumDynamicSlotGroupID Enum_Var1 = ((IDynamicSlotGroup)value1).dynamicSlot;
                EnumDynamicSlotGroupID Enum_Var2 = ((IDynamicSlotGroup)value2).dynamicSlot;
                if (Enum_Var1 == EnumDynamicSlotGroupID.Both || Enum_Var2 == EnumDynamicSlotGroupID.Both) return EnumDynamicSlotGroupID.Both;
                else if (Enum_Var1 == EnumDynamicSlotGroupID.NA && Enum_Var2 == EnumDynamicSlotGroupID.NA) return EnumDynamicSlotGroupID.NA;
                else if ((Enum_Var1 == EnumDynamicSlotGroupID.Group_1 || Enum_Var1 == EnumDynamicSlotGroupID.NA) && (Enum_Var2 == EnumDynamicSlotGroupID.NA || Enum_Var2 == EnumDynamicSlotGroupID.Group_1)) return EnumDynamicSlotGroupID.Group_1;
                else if ((Enum_Var1 == EnumDynamicSlotGroupID.Group_2 || Enum_Var1 == EnumDynamicSlotGroupID.NA) && (Enum_Var2 == EnumDynamicSlotGroupID.NA || Enum_Var2 == EnumDynamicSlotGroupID.Group_2)) return EnumDynamicSlotGroupID.Group_2;
                else return EnumDynamicSlotGroupID.Both;
            }
            set => throw new NotImplementedException();
        }

        public virtual bool GetVerdict(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            switch (op)
            {
                case EnumCompareOperator.Equal: return value1.GetValue(DynamicSlot_1, DynamicSlot_2) == value2.GetValue(DynamicSlot_1, DynamicSlot_2);
                case EnumCompareOperator.Greater: return value1.GetValue(DynamicSlot_1, DynamicSlot_2) > value2.GetValue(DynamicSlot_1, DynamicSlot_2);
                case EnumCompareOperator.GreaterEqual: return value1.GetValue(DynamicSlot_1, DynamicSlot_2) >= value2.GetValue(DynamicSlot_1, DynamicSlot_2);
                case EnumCompareOperator.NotEqual: return value1.GetValue(DynamicSlot_1, DynamicSlot_2) != value2.GetValue(DynamicSlot_1, DynamicSlot_2);
                case EnumCompareOperator.Smaller: return value1.GetValue(DynamicSlot_1, DynamicSlot_2) < value2.GetValue(DynamicSlot_1, DynamicSlot_2);
                case EnumCompareOperator.SmallerEqual: return value1.GetValue(DynamicSlot_1, DynamicSlot_2) <= value2.GetValue(DynamicSlot_1, DynamicSlot_2);
                default:
                    Log.Message("FATAL");
                    return false;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref visible, "visible");
            Scribe_Values.Look(ref op, "op");
            Scribe_Deep.Look(ref value1, "value1");
            Scribe_Deep.Look(ref value2, "value2");
        }

        public Leaf_Logic(ILogicObjeckt obj1, ILogicObjeckt obj2, EnumCompareOperator eoperator, string initName = "Leaf_Logic", bool vis = true)
        {
            value1 = obj1;
            value2 = obj2;
            op = eoperator;
            name = initName;
            visible = vis;
        }

        public Leaf_Logic()
        {

        }


    }


    public enum EnumBinaryAlgebra
    {
        bNA,
        bOR,
        bAND,
        bNOT,
        bBracketOpen,
        bBracketClose

    }

    /// <summary>
    /// None of the Expression Tree
    /// </summary>
    public class Tree_node : IExposable
    {
        public bool IsLeaf
        {
            get
            {
                if (Left == null && Right == null) return true;
                return false;
            }
        }


        public Leaf_Logic Leaf_Logic_ref = null;
        public Nullable<bool> GetValue(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            if (Leaf_Logic_ref == null) return null;
            return Leaf_Logic_ref.GetVerdict(DynamicSlot_1, DynamicSlot_2);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref Left, "Left");
            Scribe_Deep.Look(ref Right, "Right");
            Scribe_Values.Look(ref Algebra, "Algebra");
            Scribe_Deep.Look(ref Leaf_Logic_ref, "Leaf_Logic_ref");
        }

        public EnumBinaryAlgebra Algebra = EnumBinaryAlgebra.bNA;

        public Tree_node Left = null;
        public Tree_node Right = null;

        public Tree_node(EnumBinaryAlgebra algebra, Leaf_Logic Value)
        {
            this.Algebra = algebra;
            Leaf_Logic_ref = Value;
        }

        public Tree_node()
        {

        }

    }


    //The entire Tree
    public class Tree : IDynamicSlotGroup, IExposable
    {
        Tree_node rootNode;

        /* This is the infix Expression as provided by the user
         * This shall only be updated in one case: The user Edits the Expression vie the Graphical Box Interface
         * In that case this Expression will be rewritten by the Computer
         * 
         * Generating this from Postfix / the Tree should be avoided unless the above conditions appaies.
         * In all other cases this Expression will match the tree & Therefor Generating it from the Tree would only cause issues
         * - Bracket Placement
         * - Speed
         */
        public List<Tree_node> UserInfixExpr;
        public bool CheckUserInfixExpr()
        {
            if (UserInfixExpr.Count == 0) return false;
            //For evry Opened Bracket there needs to be a closed one
            if (UserInfixExpr.Where(e => e.Algebra == EnumBinaryAlgebra.bBracketOpen).Count() != UserInfixExpr.Where(e => e.Algebra == EnumBinaryAlgebra.bBracketClose).Count())
            {
                return false;
            }
            //Check that the last Value is not Algebra (closed backet is allowed in the Last Spot)
            if (!(UserInfixExpr.Last().Algebra == EnumBinaryAlgebra.bNA || UserInfixExpr.Last().Algebra == EnumBinaryAlgebra.bBracketClose))
            {
                return false;
            }

            //All else should be fine
            return true;

        }

        public bool UsesDynamicSlotGroup => throw new NotImplementedException();


        private EnumDynamicSlotGroupID GetSlotGroupOfTree(Tree_node node)
        {
            if (node == null) return EnumDynamicSlotGroupID.NA;


            EnumDynamicSlotGroupID self = node.Leaf_Logic_ref?.dynamicSlot ?? EnumDynamicSlotGroupID.NA;
            if (self == EnumDynamicSlotGroupID.Both) return EnumDynamicSlotGroupID.Both;
            if (node.Right == null && node.Left == null) return self;

            EnumDynamicSlotGroupID enum_Right = GetSlotGroupOfTree(node.Right);
            EnumDynamicSlotGroupID enum_Left = GetSlotGroupOfTree(node.Left);

            if (enum_Right == EnumDynamicSlotGroupID.Both || enum_Left == EnumDynamicSlotGroupID.Both) return EnumDynamicSlotGroupID.Both;

            if (enum_Right == EnumDynamicSlotGroupID.NA && enum_Left == EnumDynamicSlotGroupID.NA) return self;

            if ((enum_Right == EnumDynamicSlotGroupID.Group_1 || enum_Right == EnumDynamicSlotGroupID.NA) && (enum_Left == EnumDynamicSlotGroupID.NA || enum_Left == EnumDynamicSlotGroupID.Group_1))
            {
                return EnumDynamicSlotGroupID.Group_1;
            }
            if ((enum_Right == EnumDynamicSlotGroupID.Group_2 || enum_Right == EnumDynamicSlotGroupID.NA) && (enum_Left == EnumDynamicSlotGroupID.NA || enum_Left == EnumDynamicSlotGroupID.Group_2))
            {
                return EnumDynamicSlotGroupID.Group_2;
            }
            return EnumDynamicSlotGroupID.Both;

        }


        public EnumDynamicSlotGroupID dynamicSlot
        {
            get
            {
                return GetSlotGroupOfTree(rootNode);
            }
            set => throw new NotImplementedException();
        }

        //Constructs a Tree from Postfix (RPN)
        private static Tree_node BuildTree(List<Tree_node> input)
        {
            Stack<Tree_node> mystack = new Stack<Tree_node>();
            for (int i = 0; i < input.Count; i++)
            {
                Tree_node elenet = input[i];
                if (elenet.Algebra == EnumBinaryAlgebra.bNA)
                {
                    mystack.Push(elenet);
                }
                else
                {
                    //Adding the not special Code
                    if (elenet.Algebra == EnumBinaryAlgebra.bNOT)
                    {
                        elenet.Right = mystack.Pop();
                    }
                    else
                    {
                        elenet.Right = mystack.Pop();
                        elenet.Left = mystack.Pop();
                    }
                    mystack.Push(elenet);
                }
            }
            return mystack.Pop();
        }

        //Converst to Postfix (RPN)
        private static List<Tree_node> ConvertToPostfix(List<Tree_node> input)
        {

            List<Tree_node> returnList = new List<Tree_node>();
            Stack<Tree_node> myStack = new Stack<Tree_node>();

            for (int i = 0; i < input.Count; i++)
            {
                Tree_node Celement = input[i];
                if (Celement.Algebra == EnumBinaryAlgebra.bBracketOpen)
                {
                    myStack.Push(Celement);
                }
                else if (Celement.Algebra == EnumBinaryAlgebra.bNA)
                {
                    returnList.Add(Celement);
                }
                else if (Celement.Algebra != EnumBinaryAlgebra.bBracketClose)
                {
                myMarker:
                    if (myStack.Count == 0)
                    {
                        myStack.Push(Celement);
                    }
                    else
                    {
                        if (Celement.Algebra > myStack.Peek().Algebra)
                        {
                            myStack.Push(Celement);
                        }
                        else
                        {
                            returnList.Add(myStack.Pop());
                            goto myMarker;
                        }
                    }
                }
                else //Can only be Closing bracket
                {
                    while (myStack.Peek().Algebra != EnumBinaryAlgebra.bBracketOpen)
                    {
                        returnList.Add(myStack.Pop());
                    }
                    myStack.Pop();
                }
            }

            while (myStack.Count > 0)
            {
                returnList.Add(myStack.Pop());
            }

            return returnList;
        }


        public bool EvaluateTree(DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {
            return Eval(rootNode, DynamicSlot_1, DynamicSlot_2) ?? false;
        }


        private static Nullable<bool> Eval(Tree_node node, DynamicSlotGroup DynamicSlot_1, DynamicSlotGroup DynamicSlot_2)
        {

            if (node == null) return null;
            if (node.IsLeaf)
            {
                return node.GetValue(DynamicSlot_1, DynamicSlot_2) ?? false;
            }

            Nullable<bool> right = Eval(node.Right, DynamicSlot_1, DynamicSlot_2);
            Nullable<bool> left = Eval(node.Left, DynamicSlot_1, DynamicSlot_2);

            switch (node.Algebra)
            {
                case EnumBinaryAlgebra.bAND:
                    return (right ?? false) && (left ?? false);
                case EnumBinaryAlgebra.bOR:
                    return (right ?? false) || (left ?? false);
                case EnumBinaryAlgebra.bNOT:
                    if (right != null)
                    {
                        return !(bool)right;
                    }
                    else if (left != null)
                    {
                        return !(bool)left;
                    }
                    else
                    {
                        Log.Message("FATAL 1");
                        return false;
                    }
                default:
                    Log.Message("FATAL 2: " + node.Algebra);


                    return false;
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref UserInfixExpr, "UserInfixExpr", LookMode.Deep);
            Scribe_Deep.Look(ref rootNode, "rootNode");
        }

        /// <summary>
        /// Constructs the Tree
        /// </summary>
        /// <param name="input">Infix Expression</param>
        public Tree(List<Tree_node> input)
        {
            UserInfixExpr = input; //Why C# Why....
            rootNode = BuildTree(ConvertToPostfix(input));
        }

        public Tree()
        {

        }
    }



    public class LogicSignal : IDynamicSlotGroup, IExposable
    {
        //Name of the Logic Signal
        public string Name = "PRF_LogicController_Logic_Signal_Default_Name".Translate();

        public bool UsesDynamicSlotGroup => dynamicSlot != EnumDynamicSlotGroupID.NA;
        public EnumDynamicSlotGroupID dynamicSlot { get => logicTree.dynamicSlot; set => throw new NotImplementedException(); }


        public int GetValue(DynamicSlotGroup DynamicSlot_1 = null, DynamicSlotGroup DynamicSlot_2 = null)
        {

            if (logicTree.EvaluateTree(DynamicSlot_1, DynamicSlot_2))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private bool availableCrossMap = false;
        public bool AvailableCrossMap { get => availableCrossMap; set => availableCrossMap = value; }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "Name");
            Scribe_Values.Look(ref availableCrossMap, "availableCrossMap");
            Scribe_Deep.Look(ref logicTree, "logicTree");
        }

        public List<Tree_node> TreeUserInfixExp
        {
            get => logicTree.UserInfixExpr; set => logicTree.UserInfixExpr = value;
        }

        public bool UserInfixValid { get => logicTree.CheckUserInfixExpr(); }

        private Tree logicTree;

        public LogicSignal(Tree tree, string initialName = "Logic Signal")
        {
            Name = initialName;
            logicTree = tree;
        }

        public LogicSignal()
        {

        }

    }
}