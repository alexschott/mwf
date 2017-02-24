using SQLite.Net.Attributes;
using Cirrious.MvvmCross.Test.Core;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MWF.Mobile.Tests.Converters
{
    public class JsonWrappedListConverterTests
        : MvxIoCSupportingTest
    {
        private IFixture _fixture;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

        }

        [Fact]
        public void JsonWrappedListConverter_Primitive_SingleObject()
        {
            string jsonString = @"{""barcodes"":{""barcode"":""506010A032001855533201""}}";

            var deserializeJson = JsonConvert.DeserializeObject<TestItem>(jsonString);

            Assert.Equal(1, deserializeJson.Barcodes.Count);
            Assert.Equal("506010A032001855533201", deserializeJson.Barcodes.First());
        }

        [Fact]
        public void JsonWrappedListConverter_Primitive_ListObject()
        {
            string jsonString = @"{""barcodes"": { ""barcode"": [   ""506012A076001852453401"",   ""506012A076001852453402"", ""506012A076001852453403""]}}";

            var deserializeJson = JsonConvert.DeserializeObject<TestItem>(jsonString);

            Assert.Equal(3, deserializeJson.Barcodes.Count);
            Assert.Equal("506012A076001852453401", deserializeJson.Barcodes[0]);
            Assert.Equal("506012A076001852453402", deserializeJson.Barcodes[1]);
            Assert.Equal("506012A076001852453403", deserializeJson.Barcodes[2]);
        }

        [Fact]
        public void JsonWrappedListConverter_Primitive_EmptyListObject()
        {
            string jsonString = @"{""barcodes"": { ""barcode"": [ ]}}";

            var deserializeJson = JsonConvert.DeserializeObject<TestItem>(jsonString);
            Assert.Equal(0, deserializeJson.Barcodes.Count);

        }

        [Fact]
        public void JsonWrappedListConverter_Class_SingleObject()
        {
            string jsonString = @"{""instructions"": { ""instruction"": { ""line"":""NEXT DAY\r\n      **CUSTOMERS PAPERWORK MUST BE USED***\r\n"" }}}";

            var deserializeJson = JsonConvert.DeserializeObject<TestInstructionsContainer>(jsonString);
            Assert.Equal(1, deserializeJson.Instructions.Count());
            Assert.Equal(1, deserializeJson.Instructions.First().LinesList.Count());
            Assert.Equal("NEXT DAY\r\n      **CUSTOMERS PAPERWORK MUST BE USED***\r\n", deserializeJson.Instructions.First().LinesList.First());
        }


        [Fact]
        public void JsonWrappedListConverter_Class_NullObject()
        {
            string jsonString = @"{""instructions"": { ""instruction"": null}}";

            var deserializeJson = JsonConvert.DeserializeObject<TestInstructionsContainer>(jsonString);
            Assert.Equal(0, deserializeJson.Instructions.Count());

        }

        public class TestInstructionsContainer
        {
            [JsonProperty("instructions")]
            [JsonConverter(typeof(JsonWrappedListConverter<Core.Models.Instruction.Instruction>))]
            public List<Core.Models.Instruction.Instruction> Instructions { get; set; }
        }

        public class TestItem
        {

            [JsonProperty("barcodes")]
            [JsonConverter(typeof(JsonWrappedListConverter<string>))]
            public List<string> Barcodes { get; set; }
        }
            
    }
}
