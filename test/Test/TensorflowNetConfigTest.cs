using System;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Protobuf.Text;
using Tensorflow.Models.ObjectDetection.Protos;
using Xunit;

namespace Test
{

    public class TensorflowNetConfigTest
    {
        [Fact]
        public void TestTrainEvalPipelineConfig()
        {
            var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Config", "faster_rcnn_resnet101_voc07.config"));
            var parsed = TextParser.Default.Parse<TrainEvalPipelineConfig>(text);
            Assert.NotNull(parsed);
            Assert.NotNull(parsed.Model);
        }
    }
}