using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;


namespace ProjectRimFactory.Industry
{

    // grouping
    // (A && B) || C
    // (A && B) is a group

    //Logic
    //And
    // A && B
    //
    //Or
    // A || B

    //Refrences / Compare
    //We compare The counts of thing Filters and unser defined Values
    //
    //User Defined Value
    // 1; 55; 100;
    //
    //Thing filter
    //Can be restricted to a Zone / Building_Storage
    // Components in DSU
    // Steel on the Map
    //
    //Can also be a signal

    //Interface for Logic Ojeckts
    //A Logic Ojeckt is a Opjeckt that can Compared with: == != < > <= >=
    public interface ILogicObjeckt
    {
        public int Value { get; }
        public string Name { get; set; }
    }

    //Base Class
    abstract class ValueRefrence : ILogicObjeckt
    {
        abstract public int Value { get; set; }

        abstract public string Name { get; set; }

        abstract public bool Visible { get; }


    }

    /// <summary>
    /// ValueRefrence_Fixed Is a Possible Object for the Leaf_Logic Constructor
    /// This one is Used for Fixed Values that the user can Enter
    /// </summary>
    class ValueRefrence_Fixed : ValueRefrence
    {
        private int pvalue;

        private string name;
        public override int Value { get => pvalue; set => pvalue = value; }
        public override string Name { get => name; set => name = value; }

        private bool visible = true;
        public override bool Visible => visible;

        public ValueRefrence_Fixed(int initalVal = 0, string Name = "ValueRefrence_Fixed" , bool vis = true)
        {
            visible = vis;
            pvalue = initalVal;
            name = Name;
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
        public override int Value { get => logicSignal.Value; set => throw new NotImplementedException(); }

        private bool visible = true;
        public override bool Visible => visible;

        public ValueRefrence_Signal(LogicSignal signal)
        {
            logicSignal = signal;
        }
    }

    /// <summary>
    /// ValueRefrence_Fixed Is a Possible Object for the Leaf_Logic Constructor
    /// This one is Used for Comparing against Thing Counts within a predefind zone
    /// </summary>
    class ValueRefrence_ThingCount : ValueRefrence
    {
        public override int Value { get => storage.GetItemCount(filter,map); set => throw new NotImplementedException(); }

        private string name;

        public override string Name { get => name; set => name = value; }

        //Holds the filter
        public ThingFilter filter;

        public StorageLocation storage;


        private Map map;

        private bool visible = true;
        public override bool Visible => visible;

        public ValueRefrence_ThingCount(ThingFilter thingFilter, StorageLocation storageLocation, Map thismap, string Name = "ValueRefrence_ThingCount")
        {
            filter = thingFilter;
            storage = storageLocation;
            map = thismap;
            name = Name;
        }

    }

    class StorageLocation
    {
        public SlotGroup SlotGroup = null;

        public string GetLocationName()
        {
            if (SlotGroup != null) return SlotGroup.parent.SlotYielderLabel();
            return "Entire Map";
        }

        public int GetItemCount(ThingFilter filter , Map map)
        {
            int returnval = 0;
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
    class Leaf_Logic
    {
        private string name = "Leaf_Logic";
        public string Name { get => name; set => name = value; }

        private ILogicObjeckt value1;
        private ILogicObjeckt value2;
        private EnumCompareOperator op;

        public ILogicObjeckt Value1 { get => value1; set => value1 = value; }
        public ILogicObjeckt Value2 { get => value2; set => value2 = value; }
        public EnumCompareOperator Operator { get => op; set => op = value; }


        public virtual bool GetVerdict()
        {
            switch (op)
            {
                case EnumCompareOperator.Equal: return value1.Value == value2.Value;
                case EnumCompareOperator.Greater: return value1.Value > value2.Value;
                case EnumCompareOperator.GreaterEqual: return value1.Value >= value2.Value;
                case EnumCompareOperator.NotEqual: return value1.Value != value2.Value;
                case EnumCompareOperator.Smaller: return value1.Value < value2.Value;
                case EnumCompareOperator.SmallerEqual: return value1.Value <= value2.Value;
                default:
                    Log.Message("FATAL");
                    return false;
            }
        }

        public Leaf_Logic(ILogicObjeckt obj1, ILogicObjeckt obj2, EnumCompareOperator eoperator, string initName = "Leaf_Logic")
        {
            value1 = obj1;
            value2 = obj2;
            op = eoperator;
            name = initName;
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

        public Nullable<bool> Value = null;
        public EnumBinaryAlgebra Algebra = EnumBinaryAlgebra.bNA;

        public Tree_node Left = null;
        public Tree_node Right = null;

        public Tree_node(EnumBinaryAlgebra algebra, Nullable<bool> Value )
        {
            this.Algebra = algebra;
            this.Value = Value;
        }

    }


    //The entire Tree
    class Tree
    {
        Tree_node rootNode;

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


        public bool EvaluateTree()
        {
            return Eval(rootNode) ?? false;
        }


        private static Nullable<bool> Eval(Tree_node node)
        {

            if (node == null) return null;
            if (node.IsLeaf)
            {
                return node.Value ?? false;
            }

            Nullable<bool> right = Eval(node.Right);
            Nullable<bool> left = Eval(node.Left);

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



    class LogicSignal : ILogicObjeckt
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

        string ILogicObjeckt.Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private Tree logicTree;

        public LogicSignal(Tree tree)
        {
            logicTree = tree;
        }

    }



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
            leaf_Logics.Add(new Leaf_Logic(valueRefrences[0], valueRefrences[1], EnumCompareOperator.Greater));


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
