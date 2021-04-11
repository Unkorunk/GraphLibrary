using System;

namespace GraphLibrary
{
    public interface IDepartment
    {
        IDepartment Perform(StampList stampList);
    }

    public class UnconditionalDepartment : IDepartment
    {
        public IDepartment NextDepartment { get; private set; }
        public Stamp NewStamp { get; set; }
        public Stamp DeleteStamp { get; set; }
        
        /**
         * Bad constructor.
         * Use only to create loop.
         */
        public UnconditionalDepartment() {}

        /**
         * Bad method.
         * Use only to create loop.
         */
        public void SetNextDepartment(IDepartment nextDepartment)
        {
            NextDepartment = nextDepartment;
        }

        public UnconditionalDepartment(IDepartment nextDepartment)
        {
            NextDepartment = nextDepartment;
        }

        public IDepartment Perform(StampList stampList)
        {
            stampList.AddStamp(NewStamp);
            stampList.DeleteStamp(DeleteStamp);
            return NextDepartment;
        }
    }

    public class ConditionalDepartment : IDepartment
    {
        public Stamp StampCondition { get; }
        public UnconditionalDepartment IfDepartment { get; }
        public UnconditionalDepartment ElseDepartment { get; }
        
        public ConditionalDepartment(Stamp stampCondition, UnconditionalDepartment ifDepartment,
            UnconditionalDepartment elseDepartment)
        {
            StampCondition = stampCondition;
            IfDepartment = ifDepartment;
            ElseDepartment = elseDepartment;
        }

        public IDepartment Perform(StampList stampList) =>
            stampList.Contains(StampCondition) ? IfDepartment.Perform(stampList) : ElseDepartment.Perform(stampList);
    }

    public class FinishDepartment : IDepartment
    {
        public IDepartment Perform(StampList stampList) =>
            throw new Exception("Some kind of error in the library");
    }
}