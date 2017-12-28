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

            var resultExtractor = new OutputTerminator<int>();

            Task.Delay(500).Wait();

            resultExtractor.Add(node.Outputs[0]);

            Assert.Equal(expected: 4, actual: resultExtractor.First());
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

            var resultExtractor = new OutputTerminator<int>();

            Task.Delay(1000).Wait();

            resultExtractor.Add(node.Outputs[0]);

            Assert.Equal(expected: 4, actual: resultExtractor.First());
            Assert.Equal(expected: 6, actual: resultExtractor.Skip(1).First());
        }
    }
}
