using Lidsvaldr.WorkflowComponents.Arguments;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.ModelTask
{
    public class ImageConverter
    {
        delegate short Byte2Short(byte image8);
        delegate byte Short2Byte(short image16);
        delegate short One2One<T>(T image);
        delegate short Many2One<T>(T image1, T image2);
        delegate void One2Many<T>(T image, int count, out IEnumerable<T> res);

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Random rand = new Random();


        public void PhotoSlicing(byte photo, int count)
        {
            _logger.Info($"Start photo slicing. Input: {photo}, slice count: {count}");

            var photoSlicingNode = new One2Many<byte>(PhotoSlicing).ToNode();

            photoSlicingNode.Inputs[0].Add(photo);
            photoSlicingNode.Inputs[1].Add(count);

            var resultExtractor = photoSlicingNode.Outputs[0].Terminate<IEnumerable<byte>>();

            resultExtractor.WaitForResults(1);

            var res = resultExtractor.Results().First();
            _logger.Info($"End photo slicing. Output length: {res.Count()}. Output: {String.Join(", ", res)}");
        }

        public void PhotoPostprocessing(byte photo)
        {
            _logger.Info($"Start photo postprocessing. Input: {photo}");

            var convert8to16Node = new Byte2Short(Convert8bitTo16bit).ToNode();
            var colorCorrectionNode = new One2One<short>(AutoColor).ToNode();
            var highPassNode = new One2One<short>(HighPass).ToNode();
            var mergeImagesNode = new Many2One<short>(MergeImages).ToNode();
            var convert16to8Node = new Short2Byte(Convert16bitTo8bit).ToNode();

            convert16to8Node.Inputs[0].Add(mergeImagesNode.Outputs[0]);

            mergeImagesNode.Inputs[0].Add(colorCorrectionNode.Outputs[0]);
            mergeImagesNode.Inputs[1].Add(highPassNode.Outputs[0]);

            convert8to16Node.Outputs[0].ExclusiveModeEnabled = false;

            colorCorrectionNode.Inputs[0].Add(convert8to16Node.Outputs[0]);
            highPassNode.Inputs[0].Add(convert8to16Node.Outputs[0]);

            convert8to16Node.Inputs[0].Add(photo);

            var resultExtractor = convert16to8Node.Outputs[0].Terminate<byte>();

            resultExtractor.WaitForResults(1);
            _logger.Info($"End photo postprocessing. Otput: {resultExtractor.Results().First()}");
        }

        private short Convert8bitTo16bit(byte image8)
        {
            _logger.Info($"Start converting image from 8 bit to 16 bit. Input: {image8}");
            var image16 = Convert.ToInt16(image8);
            Task.Delay(rand.Next(1000, 3000)).Wait();
            _logger.Info($"End converting image from 8 bit to 16 bit. Output: {image16}");
            return image16;
        }

        private byte Convert16bitTo8bit(short image16)
        {
            _logger.Info($"Start converting image from 16 bit to 8 bit. Input: {image16}");
            var image8 = Convert.ToByte(image16);
            Task.Delay(rand.Next(1000, 3000)).Wait();
            _logger.Info($"End converting image from 16 bit to 8 bit. Output: {image8}");
            return image8;
        }

        private short AutoColor(short image)
        {
            _logger.Info($"Start auto color correction. Input: {image}");
            short correctImage = Convert.ToInt16(image * (rand.Next(2, 5)));
            Task.Delay(rand.Next(1000, 3000)).Wait();
            _logger.Info($"End auto color correction. Output: {correctImage}");
            return correctImage;
        }

        private short HighPass(short image)
        {
            _logger.Info($"Start high pass filter. Input: {image}");
            short correctImage = Convert.ToInt16(image * (rand.Next(6, 10)));
            Task.Delay(rand.Next(1000, 3000)).Wait();
            _logger.Info($"End high pass filter. Output: {correctImage}");
            return correctImage;
        }

        private short MergeImages(short image1, short image2)
        {
            _logger.Info($"Start merge images. Input: {image1}, {image2}");
            var res = image1 + image2;
            Task.Delay(rand.Next(1000, 3000)).Wait();
            _logger.Info($"End merge images. Output: {res}");
            return Convert.ToInt16(res);
        }

        private void PhotoSlicing(byte image, int count, out IEnumerable<byte> res)
        {
            _logger.Info($"Start photo slicing. Input: {image}, count: {count}");
            res = Enumerable.Repeat(image, count);
            res = res.Select((val, index) => (byte)(val * (index + 1)));
            Task.Delay(rand.Next(1000, 5000)).Wait();
            _logger.Info($"End photo slicing. Output: {String.Join(", ", res)}");
        }
    }
}
