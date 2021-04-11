using System.Collections.Generic;
using System.Linq;
using GraphLibrary;
using NUnit.Framework;

namespace TestProject
{
    [TestFixture]
    public class Tests
    {
        private static bool IsEqual(IReadOnlyList<IReadOnlyList<Stamp>> first,
            IReadOnlyList<IReadOnlyList<Stamp>> second)
        {
            if (first.Count != second.Count)
                return false;

            foreach (var innerList1 in first)
            {
                var isFound = false;

                foreach (var innerList2 in second)
                    isFound |= innerList1.Count == innerList2.Count && innerList1.All(innerList2.Contains);

                if (!isFound)
                    return false;
            }

            return true;
        }

        // d1 (+s) -> d2 ![d1s] -> d3
        [Test]
        public void Test1()
        {
            var department3 = new FinishDepartment();
            var department2 = new UnconditionalDepartment(department3);
            var department1 = new UnconditionalDepartment(department2) {NewStamp = new Stamp()};

            var graphWorker = new GraphWorker(
                startDepartment: department1,
                finishDepartment: department3,
                targetDepartment: department2
            );

            graphWorker.Start();
            graphWorker.Wait();

            IReadOnlyList<IReadOnlyList<Stamp>> output = new List<List<Stamp>>
            {
                new List<Stamp> {department1.NewStamp}
            };
            Assert.True(IsEqual(graphWorker.GetResult(), output));
            Assert.True(!graphWorker.IsEndlessLoop());
        }

        // d1+ -> d2+ -> d3 (-d2s) -> d4+ ![d1s, d4s] -> d5
        [Test]
        public void Test2()
        {
            var department5 = new FinishDepartment();
            var department4 = new UnconditionalDepartment(department5) {NewStamp = new Stamp()};
            var department3 = new UnconditionalDepartment(department4);
            var department2 = new UnconditionalDepartment(department3) {NewStamp = new Stamp()};
            var department1 = new UnconditionalDepartment(department2) {NewStamp = new Stamp()};

            department3.DeleteStamp = department2.NewStamp;

            var graphWorker = new GraphWorker(
                startDepartment: department1,
                finishDepartment: department5,
                targetDepartment: department4
            );

            graphWorker.Start();
            graphWorker.Wait();

            IReadOnlyList<IReadOnlyList<Stamp>> output = new List<List<Stamp>>
            {
                new List<Stamp> {department1.NewStamp, department4.NewStamp}
            };
            Assert.True(IsEqual(graphWorker.GetResult(), output));
            Assert.True(!graphWorker.IsEndlessLoop());
        }

        // if d1s then go from d2 to d3
        // d1+ -> d2+ --> d3+ --> d5 ![d1s, d2s_if, d3s]
        //            \-> d4+ -/
        [Test]
        public void Test3()
        {
            var department1Stamp = new Stamp();

            var department5 = new FinishDepartment();
            var department4 = new UnconditionalDepartment(department5) {NewStamp = new Stamp()};
            var department3 = new UnconditionalDepartment(department5) {NewStamp = new Stamp()};
            var department2 = new ConditionalDepartment(
                stampCondition: department1Stamp,
                ifDepartment: new UnconditionalDepartment(department3) {NewStamp = new Stamp()},
                elseDepartment: new UnconditionalDepartment(department4) {NewStamp = new Stamp()}
            );
            var department1 = new UnconditionalDepartment(department2) {NewStamp = department1Stamp};

            var graphWorker = new GraphWorker(
                startDepartment: department1,
                finishDepartment: department5,
                targetDepartment: department5
            );

            graphWorker.Start();
            graphWorker.Wait();

            IReadOnlyList<IReadOnlyList<Stamp>> output = new List<List<Stamp>>
            {
                new List<Stamp> {department1.NewStamp, department2.IfDepartment.NewStamp, department3.NewStamp}
            };
            Assert.True(IsEqual(graphWorker.GetResult(), output));
            Assert.True(!graphWorker.IsEndlessLoop());
        }

        // if d5s then go from d3 to d4
        // d1 -> d2 -> d3 -> d4 ![]
        //     \---<---/
        [Test]
        public void TestEndlessLoop1()
        {
            var department5Stamp = new Stamp();

            var department4 = new FinishDepartment();
            var department3 = new ConditionalDepartment(
                stampCondition: department5Stamp,
                ifDepartment: new UnconditionalDepartment(department4),
                elseDepartment: new UnconditionalDepartment()
            );
            var department2 = new UnconditionalDepartment(department3);
            var department1 = new UnconditionalDepartment(department2);

            department3.ElseDepartment.SetNextDepartment(department2);

            var graphWorker = new GraphWorker(
                startDepartment: department1,
                finishDepartment: department4,
                targetDepartment: department4
            );

            graphWorker.Start();
            graphWorker.Wait();

            IReadOnlyList<IReadOnlyList<Stamp>> output = new List<List<Stamp>>();
            Assert.True(IsEqual(graphWorker.GetResult(), output));
            Assert.True(graphWorker.IsEndlessLoop());
        }

        // if d4s then go from d3 to d5 else d4
        // d1+ ---> d2+ -----> d3+ -> d5 ![d1s, d2s, d3s_else, d4s, d3s_if]
        //       \---- d4+ <---/
        [Test]
        public void TestLoop1()
        {
            var department5 = new FinishDepartment();
            var department4 = new UnconditionalDepartment() {NewStamp = new Stamp()};
            var department3 = new ConditionalDepartment(
                stampCondition: department4.NewStamp,
                ifDepartment: new UnconditionalDepartment(department5) {NewStamp = new Stamp()},
                elseDepartment: new UnconditionalDepartment(department4) {NewStamp = new Stamp()}
            );
            var department2 = new UnconditionalDepartment(department3) {NewStamp = new Stamp()};
            var department1 = new UnconditionalDepartment(department2) {NewStamp = new Stamp()};

            department4.SetNextDepartment(department2);

            var graphWorker = new GraphWorker(
                startDepartment: department1,
                finishDepartment: department5,
                targetDepartment: department5
            );

            graphWorker.Start();
            graphWorker.Wait();

            IReadOnlyList<IReadOnlyList<Stamp>> output = new List<List<Stamp>>
            {
                new List<Stamp>
                {
                    department1.NewStamp, department2.NewStamp, department3.ElseDepartment.NewStamp,
                    department4.NewStamp, department3.IfDepartment.NewStamp
                }
            };
            Assert.True(IsEqual(graphWorker.GetResult(), output));
            Assert.True(!graphWorker.IsEndlessLoop());
        }

        // if d7s then go from d3 to d4 else d7
        // if d8s then go from d4 to d5 else d8
        // if d9s then go from d5 to d6 else d9
        // d1 ---> d2+ ! ---> d3+ ---> d4+ ---> d5+ ---> d6
        //          \-<- d7+ <-/       /       /
        //           \-<- d8+ <-------/       /
        //            \-<- d9+ <-------------/
        [Test]
        public void TestLoop2()
        {
            var d9 = new UnconditionalDepartment() {NewStamp = new Stamp()};
            var d8 = new UnconditionalDepartment() {NewStamp = new Stamp()};
            var d7 = new UnconditionalDepartment() {NewStamp = new Stamp()};

            var d6 = new FinishDepartment();

            var d5 = new ConditionalDepartment(
                stampCondition: d9.NewStamp,
                ifDepartment: new UnconditionalDepartment(d6) {NewStamp = new Stamp()},
                elseDepartment: new UnconditionalDepartment(d9) {NewStamp = new Stamp()}
            );
            var d4 = new ConditionalDepartment(
                stampCondition: d8.NewStamp,
                ifDepartment: new UnconditionalDepartment(d5) {NewStamp = new Stamp()},
                elseDepartment: new UnconditionalDepartment(d8) {NewStamp = new Stamp()}
            );
            var d3 = new ConditionalDepartment(
                stampCondition: d7.NewStamp,
                ifDepartment: new UnconditionalDepartment(d4) {NewStamp = new Stamp()},
                elseDepartment: new UnconditionalDepartment(d7) {NewStamp = new Stamp()}
            );
            var d2 = new UnconditionalDepartment(d3) {NewStamp = new Stamp()};
            var d1 = new UnconditionalDepartment(d2);

            d9.SetNextDepartment(d2);
            d8.SetNextDepartment(d2);
            d7.SetNextDepartment(d2);

            var graphWorker = new GraphWorker(
                startDepartment: d1,
                finishDepartment: d6,
                targetDepartment: d2
            );

            graphWorker.Start();
            graphWorker.Wait();

            IReadOnlyList<IReadOnlyList<Stamp>> output = new List<List<Stamp>>
            {
                new List<Stamp> {d2.NewStamp},
                new List<Stamp> {d2.NewStamp, d3.ElseDepartment.NewStamp, d7.NewStamp},
                new List<Stamp>
                {
                    d2.NewStamp, d3.ElseDepartment.NewStamp, d7.NewStamp, d3.IfDepartment.NewStamp,
                    d4.ElseDepartment.NewStamp, d8.NewStamp
                },
                new List<Stamp>
                {
                    d2.NewStamp, d3.ElseDepartment.NewStamp, d7.NewStamp, d3.IfDepartment.NewStamp,
                    d4.ElseDepartment.NewStamp, d8.NewStamp, d4.IfDepartment.NewStamp, d5.ElseDepartment.NewStamp,
                    d9.NewStamp
                }
            };
            Assert.True(IsEqual(graphWorker.GetResult(), output));
            Assert.True(!graphWorker.IsEndlessLoop());
        }
    }
}