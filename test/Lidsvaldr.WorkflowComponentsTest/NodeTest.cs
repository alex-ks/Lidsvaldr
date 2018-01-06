using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Executer;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lidsvaldr.WorkflowComponentsTest
{
    public class NodeTest
    {
        [Fact]
        public void Successfull2Plus2Test()
        {
            Func<int, int, int> MyAdd = (x, y) =>
            {
                return x + y;
            };
            var node = new NodeExecuter(MyAdd);
            node.Inputs[0].Add(2);
            node.Inputs[1].Add(2);

            var resultExtractor = node.Outputs[0].Terminate<int>();

            resultExtractor.WaitForResults(1);

            Assert.Empty(resultExtractor.Exceptions());
            Assert.Equal(expected: 4, actual: resultExtractor.Results().First());
        }

        [Fact]
        public void SuccsessfullParallelTest()
        {
            int sleepTime = 200;
            Func<int, int, int> MyAdd = (x, y) =>
            {
                Task.Delay(sleepTime).Wait();
                sleepTime -= 100;
                return x + y;
            };
            var node = new NodeExecuter(MyAdd, 2);
            node.Inputs[0].Add(2);
            node.Inputs[0].Add(3);
            node.Inputs[1].Add(2);
            node.Inputs[1].Add(3);

            var resultExtractor = node.Outputs[0].Terminate<int>();

            resultExtractor.WaitForResults(2);

            Assert.Empty(resultExtractor.Exceptions());
            Assert.Equal(expected: 4, actual: resultExtractor.Results().First());
            Assert.Equal(expected: 6, actual: resultExtractor.Results().Skip(1).First());
        }

        [Fact]
        public void SuccessfullEarlyReturnFromWaitTest()
        {
            Func<int, int> MyWait = delay =>
            {
                Task.Delay(delay).Wait();
                return delay;
            };
            var node = new NodeExecuter(MyWait);

            node.Inputs[0].Add(5000);
            var resultExtractor = node.Outputs[0].Terminate<int>();
            resultExtractor.WaitForResults(1, 200);
            Assert.Empty(resultExtractor.Exceptions());
            Assert.Empty(resultExtractor.Results());
        }

        [Fact]
        public void SuccessfulNodeChainTest()
        {
            Func<int, int> MyInc = x => x + 1;
            Func<int, int, double> MyPow = (x, y) => Math.Pow(x, y);

            var node1 = MyInc.ToNode(name: "Inc");
            var node2 = MyPow.ToNode(name: "Pow");

            node1.Inputs[0].Add(1);
            node2.Inputs[0].Add(node1.Outputs[0]);
            node2.Inputs[1].Add(5);

            var resultExtractor = node2.Outputs[0].Terminate<double>();

            resultExtractor.WaitForResults(1);
            Assert.Empty(resultExtractor.Exceptions());
            Assert.Equal(expected: 32.0, actual: resultExtractor.Results().First());
        }

        [Fact]
        public void NodeNamingTest()
        {
            Func<int, int> MyInc = x => x + 1;
            Func<int, int, double> MyPow = (x, y) => Math.Pow(x, y);

            const string name = "Inc";
            const string generatedName = "Int32 * Int32 -> Double";

            var node1 = MyInc.ToNode(name: name);
            var node2 = MyPow.ToNode();

            Assert.Equal(expected: name, actual: node1.Name);
            Assert.Equal(expected: generatedName, actual: node2.Name);
        }
    }
}
