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

        delegate int MyDelegate(int i, float f, out double d);

        [Fact]
        public void MultipleOutputsTest()
        {
            int MyFunc(int i, float f, out double d)
            {
                d = i * f ;
                return (int)(i * f);
            }

            var node = new MyDelegate(MyFunc).ToNode();

            node.Inputs[0].Add(2);
            node.Inputs[1].Add(3.14f);

            var extractor1 = node.Outputs[0].Terminate<double>();
            var extractor2 = node.Outputs[1].Terminate<int>();

            extractor1.WaitForResults(1);
            extractor2.WaitForResults(1);

            Assert.Equal(expected: 6.28, actual: extractor1.Results().First(), precision: 6);
            Assert.Equal(expected: 6, actual: extractor2.Results().First());
        }

        [Fact]
        public void SuccessfullOutputSplitTest()
        {
            Func<int, int> myInc = x => x + 1;
            Func<int, int, int> myAdd = (x, y) => x + y;

            var inputs = new[] { 1, 2 };

            var node1 = myInc.ToNode();
            node1.Inputs[0].AddCollection(inputs);

            var node2 = myAdd.ToNode();
            node2.Inputs[0].Add(node1.Outputs[0]);
            node2.Inputs[1].Add(node1.Outputs[0]);

            var resultExtractor = node2.Outputs[0].Terminate<int>();
            resultExtractor.WaitForResults(1);

            Assert.Equal(expected: 5, actual: resultExtractor.Results().First());
        }
    }
}
