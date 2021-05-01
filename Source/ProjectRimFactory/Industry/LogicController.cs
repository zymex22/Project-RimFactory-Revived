using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;


namespace ProjectRimFactory.Industry
{

    public interface ILogicObjeckt
    {
        public int GetValue(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2);
        
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
    abstract class ValueRefrence : ILogicObjeckt , IDynamicSlotGroup
    {
        abstract public int Value { set; }

        abstract public string Name { get; set; }

        abstract public bool Visible { get; }

        virtual public bool UsesDynamicSlotGroup { get => dynamicSlot != EnumDynamicSlotGroupID.NA; }
        public abstract EnumDynamicSlotGroupID dynamicSlot { get; set; }

        abstract public int GetValue(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2);
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

        public override EnumDynamicSlotGroupID dynamicSlot { get => EnumDynamicSlotGroupID.NA ; set => throw new NotImplementedException(); }

        public ValueRefrence_Fixed(int initalVal = 0, string Name = "ValueRefrence_Fixed" , bool vis = true)
        {
            visible = vis;
            pvalue = initalVal;
            name = Name;
        }

        public override int GetValue(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2)
        {
            return pvalue;
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

        public override int GetValue(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2)
        {
            return logicSignal.GetValue(DynamicSlot_1, DynamicSlot_2);
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

        public override int GetValue(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2)
        {
            return storage.GetItemCount(filter, map, DynamicSlot_1, DynamicSlot_2);
        }
    }

    class StorageLocation : IDynamicSlotGroup
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
                    return "Dynamic 1";
                }
                else
                {
                    return "Dynamic 2";
                }
            }
            return "Entire Map";
        }


        public int GetItemCount(ThingFilter filter, Map map , SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2)
        {
            int returnval = 0;
            switch (dynamicSlot)
            {
                case EnumDynamicSlotGroupID.NA:
                {
                    if (SlotGroup != null)
                    {
                        returnval = SlotGroup.HeldThings.Where(t => filter.Allows(t)).Select(n => n.stackCount).Count();
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
                case EnumDynamicSlotGroupID.Group_1: return DynamicSlot_1.HeldThings.Where(t => filter.Allows(t)).Select(n => n.stackCount).Count();
                case EnumDynamicSlotGroupID.Group_2: return DynamicSlot_2.HeldThings.Where(t => filter.Allows(t)).Select(n => n.stackCount).Count();
                default:
                    {
                        Log.Error("PRF Logic-Controller Invalide DynamicSlot: " + dynamicSlot);
                        return returnval;
                    }

            }

            


        }

    }


    

    /// <summary>
    /// Logic Operators for Leaf_Logic
    ///  == != < > <= >=
    /// </summary>
    enum EnumCompareOperator
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
    class Leaf_Logic : IDynamicSlotGroup
    {
        private string name = "Leaf_Logic";
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

        public virtual bool GetVerdict(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2)
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

        public Leaf_Logic(ILogicObjeckt obj1, ILogicObjeckt obj2, EnumCompareOperator eoperator, string initName = "Leaf_Logic" , bool vis = true)
        {
            value1 = obj1;
            value2 = obj2;
            op = eoperator;
            name = initName;
            visible = vis;
        }


    }


    enum EnumBinaryAlgebra
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
    class Tree_node
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
        public Nullable<bool> GetValue(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2)
        {
            if (Leaf_Logic_ref == null) return null;
            return Leaf_Logic_ref.GetVerdict(DynamicSlot_1, DynamicSlot_2);
        }
        public EnumBinaryAlgebra Algebra = EnumBinaryAlgebra.bNA;

        public Tree_node Left = null;
        public Tree_node Right = null;

        public Tree_node(EnumBinaryAlgebra algebra, Leaf_Logic Value )
        {
            this.Algebra = algebra;
            Leaf_Logic_ref = Value;
        }

    }


    //The entire Tree
    class Tree : IDynamicSlotGroup
    {
        Tree_node rootNode;

        public bool UsesDynamicSlotGroup => throw new NotImplementedException();


        private EnumDynamicSlotGroupID GetSlotGroupOfTree(Tree_node node)
        {
            if (node == null) return EnumDynamicSlotGroupID.NA;

            EnumDynamicSlotGroupID enum_Right = GetSlotGroupOfTree(node.Right);
            EnumDynamicSlotGroupID enum_Left = GetSlotGroupOfTree(node.Left);

            if (enum_Right == EnumDynamicSlotGroupID.Both || enum_Left == EnumDynamicSlotGroupID.Both) return EnumDynamicSlotGroupID.Both;
            else if (enum_Right == EnumDynamicSlotGroupID.NA && enum_Left == EnumDynamicSlotGroupID.NA) return EnumDynamicSlotGroupID.NA;
            else if ((enum_Right == EnumDynamicSlotGroupID.Group_1 || enum_Right == EnumDynamicSlotGroupID.NA) && (enum_Left == EnumDynamicSlotGroupID.NA || enum_Left == EnumDynamicSlotGroupID.Group_1)) return EnumDynamicSlotGroupID.Group_1;
            else if ((enum_Right == EnumDynamicSlotGroupID.Group_2 || enum_Right == EnumDynamicSlotGroupID.NA) && (enum_Left == EnumDynamicSlotGroupID.NA || enum_Left == EnumDynamicSlotGroupID.Group_2)) return EnumDynamicSlotGroupID.Group_2;
            else return EnumDynamicSlotGroupID.Both;

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


        public bool EvaluateTree(SlotGroup DynamicSlot_1, SlotGroup DynamicSlot_2 )
        {
            return Eval(rootNode , DynamicSlot_1 , DynamicSlot_2) ?? false;
        }


        private static Nullable<bool> Eval(Tree_node node , SlotGroup DynamicSlot_1 , SlotGroup DynamicSlot_2 )
        {

            if (node == null) return null;
            if (node.IsLeaf)
            {
                return node.GetValue(DynamicSlot_1, DynamicSlot_2) ?? false;
            }

            Nullable<bool> right = Eval(node.Right, DynamicSlot_1, DynamicSlot_2);
            Nullable<bool> left = Eval(node.Left,  DynamicSlot_1, DynamicSlot_2);

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
                        Log.Message("FATAL");
                        return false;
                    }
                default:
                    Log.Message("FATAL");
                    return false;
            }
        }

        /// <summary>
        /// Constructs the Tree
        /// </summary>
        /// <param name="input">Infix Expression</param>
        public Tree(List<Tree_node> input)
        {
            rootNode = BuildTree(ConvertToPostfix(input));
        }

    }



    class LogicSignal : IDynamicSlotGroup
    {
        //Name of the Logic Signal
        public string Name = "Logic Signal";

        //The true false Value of the signal
        private bool Value_bool = false;

        public int Value
        {
            get
            {
                return Value_bool ? 1 : 0;
            }
        }

        //TODO
        public bool UsesDynamicSlotGroup => dynamicSlot != EnumDynamicSlotGroupID.NA;
        //TODO
        public EnumDynamicSlotGroupID dynamicSlot { get =>  logicTree.dynamicSlot   ; set => throw new NotImplementedException(); }

        public int GetValue(SlotGroup DynamicSlot_1 = null, SlotGroup DynamicSlot_2 = null)
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



        private Tree logicTree;

        public LogicSignal(Tree tree , string initialName = "Logic Signal")
        {
            Name = initialName;
            logicTree = tree;
        }

    }

    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    class LogicController : Building
    {
        //List of Values to be used in leaf_Logics
        public List<ValueRefrence> valueRefrences = new List<ValueRefrence>();
        //Comparisions between valueRefrences that yield True or False. To be used in the Algebra of LogicSignals Trees
        public List<Leaf_Logic> leaf_Logics = new List<Leaf_Logic>();
        //Complete Logic Signals
        public List<LogicSignal> LogicSignals = new List<LogicSignal>();


        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref LogicSignals, "signals", new object[] { this });
            Scribe_Deep.Look(ref leaf_Logics, "leaf_Logics", new object[] { this });
            Scribe_Deep.Look(ref valueRefrences, "valueRefrences", new object[] { this });
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            

            if (valueRefrences == null)
            {
                valueRefrences = new List<ValueRefrence>();
            }
            //Not really visible to the User Used for Creating New LeafLogic
            valueRefrences.Add(new ValueRefrence_Fixed(0, "Dummy-1", false));
            valueRefrences.Add(new ValueRefrence_Fixed(0, "Dummy-2", false));


            valueRefrences.Add(new ValueRefrence_Fixed(5, "SpawnTesting - 1"));
            valueRefrences.Add(new ValueRefrence_Fixed(0, "SpawnTesting - 2"));

            
            
            if (leaf_Logics == null)
            {
                leaf_Logics = new List<Leaf_Logic>();
            }
            leaf_Logics.Add(new Leaf_Logic(valueRefrences[0], valueRefrences[1], EnumCompareOperator.Greater,"Dummy-1",false));
            leaf_Logics.Add(new Leaf_Logic(valueRefrences[0], valueRefrences[1], EnumCompareOperator.Greater,"Spawn Test"));


            if (LogicSignals == null)
            {
                LogicSignals = new List<LogicSignal>();
            }
        }

        public override void Tick()
        {
            base.Tick();
        }
    }






}
