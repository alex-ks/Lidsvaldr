using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Executer;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.ModelTask
{
    class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        static Random rand = new Random();

        delegate short Convert8bitTo16bitDelegate(byte image8);
        delegate byte Convert16bitTo8bitDelegate(short image16);
        delegate short One2OneDelegate(short image);
        delegate short N2OneDelegate(short image1, short image2);
        delegate void PhotoSlicingDelegate(byte image, int count, out IEnumerable<int> res);

        static void Main(string[] args)
        {
            PhotoPostprocessing(2);
            PhotoSlicing(2, 5);
        }

        static void PhotoSlicing(byte photo, int count)
        {
            logger.Info($"Start photo slicing. Input: {photo}, slice count: {count}");

            var photoSlicingNode = new PhotoSlicingDelegate(PhotoSlicing).ToNode();

            photoSlicingNode.Inputs[0].Add(photo);
            photoSlicingNode.Inputs[1].Add(count);

            var resultExtractor = photoSlicingNode.Outputs[0].Terminate<IEnumerable<int>>();

            resultExtractor.WaitForResults(1);

            var res = resultExtractor.Results().First();
            logger.Info($"End photo slicing. Output length: {res.Count()}. Output: {String.Join(", ", res)}");
        }

        static void PhotoPostprocessing(byte photo) {
            logger.Info($"Start photo postprocessing. Input: {photo}");

            var convert8to16Node = new Convert8bitTo16bitDelegate(Convert8bitTo16bit).ToNode();
            var colorCorrectionNode = new One2OneDelegate(AutoColor).ToNode();
            var highPassNode = new One2OneDelegate(HighPass).ToNode();
            var mergeImagesNode = new N2OneDelegate(MergeImages).ToNode();
            var convert16to8Node = new Convert16bitTo8bitDelegate(Convert16bitTo8bit).ToNode();

            convert16to8Node.Inputs[0].Add(mergeImagesNode.Outputs[0]);

            mergeImagesNode.Inputs[0].Add(colorCorrectionNode.Outputs[0]);
            mergeImagesNode.Inputs[1].Add(highPassNode.Outputs[0]);

            convert8to16Node.Outputs[0].ExclusiveModeEnabled = false;

            colorCorrectionNode.Inputs[0].Add(convert8to16Node.Outputs[0]);
            highPassNode.Inputs[0].Add(convert8to16Node.Outputs[0]);

            convert8to16Node.Inputs[0].Add(photo);

            var resultExtractor = convert16to8Node.Outputs[0].Terminate<byte>();

            resultExtractor.WaitForResults(1);
            logger.Info($"End photo postprocessing. Otput: {resultExtractor.Results().First()}");

        }

        static short Convert8bitTo16bit(byte image8)
        {
            logger.Info($"Start converting image from 8 bit to 16 bit. Input: {image8}");
            var image16 = Convert.ToInt16(image8);
            Task.Delay(rand.Next(1000, 3000)).Wait();
            logger.Info($"End converting image from 8 bit to 16 bit. Output: {image16}");
            return image16;
        }

        static byte Convert16bitTo8bit(short image16)
        {
            logger.Info($"Start converting image from 16 bit to 8 bit. Input: {image16}");
            var image8 = Convert.ToByte(image16);
            Task.Delay(rand.Next(1000, 3000)).Wait();
            logger.Info($"End converting image from 16 bit to 8 bit. Output: {image8}");
            return image8;
        }

        static short AutoColor(short image) {
            logger.Info($"Start auto color correction. Input: {image}");
            short correctImage = Convert.ToInt16(image * (rand.Next(2, 5)));
            Task.Delay(rand.Next(1000, 3000)).Wait();
            logger.Info($"End auto color correction. Output: {correctImage}");
            return correctImage;
        }

        static short HighPass(short image) {
            logger.Info($"Start high pass filter. Input: {image}");
            short correctImage = Convert.ToInt16(image * (rand.Next(6, 10)));
            Task.Delay(rand.Next(1000, 3000)).Wait();
            logger.Info($"End high pass filter. Output: {correctImage}");
            return correctImage;
        }

        static short MergeImages(short image1, short image2) {
            logger.Info($"Start merge images. Input: {image1}, {image2}");
            var res = image1 + image2;
            Task.Delay(rand.Next(1000, 3000)).Wait();
            logger.Info($"End merge images. Output: {res}");
            return Convert.ToInt16(res);
        }

        static void PhotoSlicing(byte image, int count, out IEnumerable<int> res)
        {
            logger.Info($"Start photo slicing. Input: {image}, count: {count}");
            res = Enumerable.Repeat<int>(image, count);
            res = res.Select((val, index) => val * (index+1));
            Task.Delay(rand.Next(1000, 5000)).Wait();
            logger.Info($"End photo slicing. Output: {String.Join(", ", res)}");
        }
    }
}
