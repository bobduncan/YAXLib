﻿// Copyright (C) Sina Iravanian, Julian Verdurmen, axuno gGmbH and other contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using YAXLib;
using YAXLib.Enums;
using YAXLib.Exceptions;
using YAXLib.Options;
using YAXLibTests.SampleClasses;
using YAXLibTests.SampleClasses.SelfReferencingObjects;

namespace YAXLibTests
{
    [TestFixture]
    public class SerializationTest
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [Test]
        public void BasicTypeSerializationTest()
        {
            var objs = new object[] {123, 654.321, "SomeString", 24234L};
            var types = new[] {typeof(int), typeof(double), typeof(string), typeof(long)};
            var serializedResults = new[]
            {
                "<Int32>123</Int32>", "<Double>654.321</Double>", "<String>SomeString</String>", "<Int64>24234</Int64>"
            };

            for (var i = 0; i < objs.Length; i++)
            {
                var serializer = new YAXSerializer(objs[i].GetType());
                var got = serializer.Serialize(objs[i]);
                Assert.That(got, Is.EqualTo(serializedResults[i]));

                var deser = new YAXSerializer(types[i]);
                var obj = deser.Deserialize(got);
                Assert.That(objs[i], Is.EqualTo(obj));
            }
        }

        [Test]
        public void BookTest()
        {
            const string result =
                @"<!-- This example demonstrates serailizing a very simple class -->
<Book>
  <Title>Inside C#</Title>
  <Author>Tom Archer &amp; Andrew Whitechapel</Author>
  <PublishYear>2002</PublishYear>
  <Price>30.5</Price>
</Book>";
            var serializer = new YAXSerializer(typeof(Book), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(Book.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void ThreadingTest()
        {
            try
            {
                for (var i = 0; i < 100; i++)
                {
                    var th = new Thread(() =>
                        {
                            var serializer = new YAXSerializer(typeof(Book), YAXExceptionHandlingPolicies.DoNotThrow,
                                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
                            var got = serializer.Serialize(Book.GetSampleInstance());
                            var deserializer = new YAXSerializer(typeof(Book), YAXExceptionHandlingPolicies.DoNotThrow,
                                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
                            var book = deserializer.Deserialize(got) as Book;
                            Assert.That(book, Is.Not.Null);
                        }
                    );

                    th.Start();
                }
            }
            catch
            {
                Assert.Fail("Exception fired in threading method");
            }
        }

        [Test]
        public void BookWithDecimalPriceTest()
        {
            const string result =
                @"<!-- This example demonstrates serailizing a very simple class -->
<SimpleBookClassWithDecimalPrice>
  <Title>Inside C#</Title>
  <Author>Tom Archer &amp; Andrew Whitechapel</Author>
  <PublishYear>2002</PublishYear>
  <Price>32.20</Price>
</SimpleBookClassWithDecimalPrice>";
            var serializer = new YAXSerializer(typeof(SimpleBookClassWithDecimalPrice),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(SimpleBookClassWithDecimalPrice.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [TestCase("fr-FR")]
        [TestCase("fa-IR")]
        [TestCase("en-US")]
        [TestCase("en-GB")]
        [TestCase("")]
        public void Serializing_DeSerializing_Same_Culture(string cultName)
        {
            var culture = new CultureInfo(cultName);
            var instance = CultureSample.GetSampleInstance();
            var expected =
                $@"<!-- This class contains fields that are vulnerable to culture changes! -->
<CultureSample Number2=""{instance.Number2.ToString(culture)}"" Dec2=""{instance.Dec2.ToString(culture)}"" Date2=""{instance.Date2.ToString(culture)}"">
  <Number1>{instance.Number1.ToString(culture)}</Number1>
  <Number3>{instance.Number3.ToString(culture)}</Number3>
  <Numbers>
    <Double>{instance.Numbers[0].ToString(culture)}</Double>
    <Double>{instance.Numbers[1].ToString(culture)}</Double>
    <Double>{instance.Numbers[2].ToString(culture)}</Double>
  </Numbers>
  <Dec1>{instance.Dec1.ToString(culture)}</Dec1>
  <Date1>{instance.Date1.ToString(culture)}</Date1>
</CultureSample>";

            var serializer = new YAXSerializer(typeof(CultureSample),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.SerializeNullObjects,
                    Culture = new CultureInfo(cultName)
                });
            var serResult = serializer.Serialize(CultureSample.GetSampleInstance());

            CultureInfo.CurrentCulture = new CultureInfo(cultName);
            serializer = new YAXSerializer(typeof(CultureSample),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.SerializeNullObjects,
                    Culture = new CultureInfo(cultName)
                });

            var desResult = serializer.Deserialize(serResult) as CultureSample;
            Assert.That(serResult, Is.EqualTo(expected), $"Comparing serialized '{cultName}' with expected.");
            Assert.That(desResult.Equals(CultureSample.GetSampleInstance()),
                $"Comparing deserialized '{cultName}' with deserialized expected.");
        }

        [Test]
        public void Serializing_DeSerializing_Different_Culture()
        {
            var serializer = new YAXSerializer(typeof(CultureSample),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.SerializeNullObjects,
                    Culture = new CultureInfo("fr-FR")
                });
            var serResult1 = serializer.Serialize(CultureSample.GetSampleInstance());
            serializer.Options.Culture = CultureInfo.InvariantCulture;
            var serResult2 = serializer.Serialize(CultureSample.GetSampleInstance());
            
            serResult1.Should().NotBeEquivalentTo(serResult2, because:"cultures with different number formats are used");
        }
        
        [TestCase("fr-FR", "fa-IR")]
        [TestCase("", "fr-FR")]
        [TestCase("fr-FR", "en-US")]
        [TestCase("de-DE", "")]
        [TestCase("fa-IR", "en-US")]
        [TestCase("en-US", "")]
        public void YAXLib_v2_Compatibility_Using_Defaults(string cultName1, string cultName2)
        {
            var instance = CultureSample.GetSampleInstance();
            var expected =
                $@"<!-- This class contains fields that are vulnerable to culture changes! -->
<CultureSample Number2=""{instance.Number2.ToString(CultureInfo.InvariantCulture)}"" Dec2=""{instance.Dec2.ToString(CultureInfo.InvariantCulture)}"" Date2=""{instance.Date2.ToString(CultureInfo.InvariantCulture)}"">
  <Number1>{instance.Number1.ToString(CultureInfo.InvariantCulture)}</Number1>
  <Number3>{instance.Number3.ToString(CultureInfo.InvariantCulture)}</Number3>
  <Numbers>
    <Double>{instance.Numbers[0].ToString(CultureInfo.InvariantCulture)}</Double>
    <Double>{instance.Numbers[1].ToString(CultureInfo.InvariantCulture)}</Double>
    <Double>{instance.Numbers[2].ToString(CultureInfo.InvariantCulture)}</Double>
  </Numbers>
  <Dec1>{instance.Dec1.ToString(CultureInfo.InvariantCulture)}</Dec1>
  <Date1>{instance.Date1.ToString(CultureInfo.InvariantCulture)}</Date1>
</CultureSample>";
            
            CultureInfo.CurrentCulture = new CultureInfo(cultName1);
            var serializer = new YAXSerializer(typeof(CultureSample));
            var serResult = serializer.Serialize(CultureSample.GetSampleInstance());

            CultureInfo.CurrentCulture = new CultureInfo(cultName2);
            serializer = new YAXSerializer(typeof(CultureSample));
            var desResult = serializer.Deserialize(serResult) as CultureSample;
            
            serResult.Should().BeEquivalentTo(expected, because: "this is our result XML literal");
            desResult.Should().BeEquivalentTo(CultureSample.GetSampleInstance(), because: "this is the original object");
        }

        [Test]
        public void BookStructTest()
        {
            const string result =
                @"<!-- This example demonstrates serailizing a very simple struct -->
<BookStruct>
  <Title>Reinforcement Learning an Introduction</Title>
  <Author>R. S. Sutton &amp; A. G. Barto</Author>
  <PublishYear>1998</PublishYear>
  <Price>38.75</Price>
</BookStruct>";
            var serializer = new YAXSerializer(typeof(BookStruct), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(BookStruct.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void BookOrderTest()
        {
            const string result =
                @"<!-- This example demonstrates serailizing a very simple class, but with partial priority ordering. -->
<BookClassWithOrdering>
  <Author>R. S. Sutton &amp; A. G. Barto</Author>
  <Title>Reinforcement Learning an Introduction</Title>
  <PublishYear>1998</PublishYear>
  <Price>38.75</Price>
  <Review>This book is very good at being a book.</Review>
  <Publisher>MIT Press</Publisher>
  <Editor>MIT Productions</Editor>
</BookClassWithOrdering>";
            var serializer = new YAXSerializer(typeof(BookClassWithOrdering), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(BookClassWithOrdering.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void WarehouseSimpleTest()
        {
            const string result =
                @"<!-- This example is our basic hypothetical warehouse -->
<WarehouseSimple>
  <Name>Foo Warehousing Ltd.</Name>
  <Address>No. 10, Some Ave., Some City, Some Country</Address>
  <Area>120000.5</Area>
</WarehouseSimple>";
            var serializer = new YAXSerializer(typeof(WarehouseSimple), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseSimple.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void WarehouseStructuredTest()
        {
            const string result =
                @"<!-- This example shows our hypothetical warehouse, a little bit structured -->
<WarehouseStructured Name=""Foo Warehousing Ltd."">
  <SiteInfo address=""No. 10, Some Ave., Some City, Some Country"">
    <SurfaceArea>120000.5</SurfaceArea>
  </SiteInfo>
</WarehouseStructured>";
            var serializer = new YAXSerializer(typeof(WarehouseStructured), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseStructured.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void WarehouseWithArrayTest()
        {
            const string result =
                @"<!-- This example shows the serialization of arrays -->
<WarehouseWithArray Name=""Foo Warehousing Ltd."">
  <SiteInfo address=""No. 10, Some Ave., Some City, Some Country"">
    <SurfaceArea>120000.5</SurfaceArea>
  </SiteInfo>
  <StoreableItems>Item3, Item6, Item9, Item12</StoreableItems>
</WarehouseWithArray>";
            var serializer = new YAXSerializer(typeof(WarehouseWithArray), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseWithArray.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void DictionaryWithNullValue()
        {
            const string theKey = "TheKey";
            var d = new Dictionary<string, object> {{theKey, null}};
            var serializer = new YAXSerializer(typeof(Dictionary<string, object>),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.DontSerializeNullObjects);
            var got = serializer.Serialize(d);
            var gotDes = serializer.Deserialize(got) as Dictionary<string, object>;
            Assert.AreEqual(d[theKey], gotDes[theKey]);
        }

        /// <summary>
        ///     Even if we set SerializeNullObjects
        /// </summary>
        [Test]
        public void DictionaryWithNullValueShouldNotCrash()
        {
            const string theKey = "TheKey";
            var d = new Dictionary<string, object> {{theKey, null}};
            var serializer = new YAXSerializer(typeof(Dictionary<string, object>),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(d);
            Assert.AreEqual(@"<DictionaryOfStringObject>
  <KeyValuePairOfStringObject>
    <Key>TheKey</Key>
    <Value />
  </KeyValuePairOfStringObject>
</DictionaryOfStringObject>", got);
        }

        [Test]
        public void WarehouseWithDictionaryTest()
        {
            const string result =
                @"<!-- This example shows the serialization of Dictionary -->
<WarehouseWithDictionary Name=""Foo Warehousing Ltd."">
  <SiteInfo address=""No. 10, Some Ave., Some City, Some Country"">
    <SurfaceArea>120000.5</SurfaceArea>
  </SiteInfo>
  <StoreableItems>Item3, Item6, Item9, Item12</StoreableItems>
  <ItemQuantities>
    <ItemInfo Item=""Item3"" Count=""10"" />
    <ItemInfo Item=""Item6"" Count=""120"" />
    <ItemInfo Item=""Item9"" Count=""600"" />
    <ItemInfo Item=""Item12"" Count=""25"" />
  </ItemQuantities>
</WarehouseWithDictionary>";
            var serializer = new YAXSerializer(typeof(WarehouseWithDictionary), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseWithDictionary.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void WarehouseNestedObjectTest()
        {
            const string result =
                @"<!-- This example demonstrates serializing nested objects -->
<WarehouseNestedObjectExample Name=""Foo Warehousing Ltd."">
  <SiteInfo address=""No. 10, Some Ave., Some City, Some Country"">
    <SurfaceArea>120000.5</SurfaceArea>
  </SiteInfo>
  <StoreableItems>Item3, Item6, Item9, Item12</StoreableItems>
  <ItemQuantities>
    <ItemInfo Item=""Item3"" Count=""10"" />
    <ItemInfo Item=""Item6"" Count=""120"" />
    <ItemInfo Item=""Item9"" Count=""600"" />
    <ItemInfo Item=""Item12"" Count=""25"" />
  </ItemQuantities>
  <Owner SSN=""123456789"">
    <Identification Name=""John"" Family=""Doe"" />
    <Age>50</Age>
  </Owner>
</WarehouseNestedObjectExample>";
            var serializer = new YAXSerializer(typeof(WarehouseNestedObjectExample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseNestedObjectExample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void ProgrammingLanguageTest()
        {
            const string result =
                @"<!-- This example is used in the article to show YAXLib exception handling policies -->
<ProgrammingLanguage>
  <LanguageName>C#</LanguageName>
  <IsCaseSensitive>true</IsCaseSensitive>
</ProgrammingLanguage>";
            var serializer = new YAXSerializer(typeof(ProgrammingLanguage), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(ProgrammingLanguage.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void ColorExampleTest()
        {
            const string result =
                @"<!-- This example shows a technique for serializing classes without a default constructor -->
<ColorExample>
  <TheColor>#FF0000FF</TheColor>
</ColorExample>";
            var serializer = new YAXSerializer(typeof(ColorExample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(ColorExample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void MultiLevelClassTest()
        {
            const string result =
                @"<!-- This example shows a multi-level class, which helps to test -->
<!-- the null references identity problem. -->
<!-- Thanks go to Anton Levshunov for proposing this example, -->
<!-- and a discussion on this matter. -->
<MultilevelClass>
  <items>
    <FirstLevelClass>
      <ID>1</ID>
      <Second>
        <SecondID>1-2</SecondID>
      </Second>
    </FirstLevelClass>
    <FirstLevelClass>
      <ID>2</ID>
      <Second />
    </FirstLevelClass>
  </items>
</MultilevelClass>";
            var serializer = new YAXSerializer(typeof(MultilevelClass), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(MultilevelClass.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void FormattingTest()
        {
            var result =
                @"<!-- This example shows how to apply format strings to a class properties -->
<FormattingExample>
  <CreationDate>{0}</CreationDate>
  <ModificationDate>{1}</ModificationDate>
  <PI>3.14159</PI>
  <NaturalExp>
    <Double>2.718</Double>
    <Double>7.389</Double>
    <Double>20.086</Double>
    <Double>54.598</Double>
  </NaturalExp>
  <SomeLogarithmExample>
    <KeyValuePairOfDoubleDouble>
      <Key>1.50</Key>
      <Value>0.40547</Value>
    </KeyValuePairOfDoubleDouble>
    <KeyValuePairOfDoubleDouble>
      <Key>3.00</Key>
      <Value>1.09861</Value>
    </KeyValuePairOfDoubleDouble>
    <KeyValuePairOfDoubleDouble>
      <Key>6.00</Key>
      <Value>1.79176</Value>
    </KeyValuePairOfDoubleDouble>
  </SomeLogarithmExample>
</FormattingExample>";

            result = string.Format(result,
                FormattingExample.GetSampleInstance().CreationDate.ToString("D"),
                FormattingExample.GetSampleInstance().ModificationDate.ToString("d")
            );

            var serializer = new YAXSerializer(typeof(FormattingExample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(FormattingExample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void PathsExampleTest()
        {
            const string result =
                @"<!-- This example demonstrates how not to use -->
<!-- white spaces as separators while serializing -->
<!-- collection classes serially -->
<PathsExample>
  <Paths>C:\SomeFile.txt;C:\SomeFolder\SomeFile.txt;C:\Some Folder With Space Such As\Program Files</Paths>
</PathsExample>";
            var serializer = new YAXSerializer(typeof(PathsExample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(PathsExample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }


        [TestCase("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // NETFRAMEWORK2.x
        [TestCase("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // NETFRAMEWORK4.x
        [TestCase("System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")] // NETSTANDARD
        [TestCase("System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")] // NET5.0
        public void MoreComplexExample_CrossFramework_Test(string coreLibName)
        {
            var result =
                @$"<!-- This example tries to show almost all features of YAXLib which were not shown before. -->
<!-- FamousPoints - shows a dictionary with a non-primitive value member. -->
<!-- IntEnumerable - shows serializing properties of type IEnumerable<> -->
<!-- Students - shows the usage of YAXNotCollection attribute -->
<MoreComplexExample xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <FamousPoints>
    <PointInfo PName=""Center"">
      <ThePoint X=""0"" Y=""0"" />
    </PointInfo>
    <PointInfo PName=""Q1"">
      <ThePoint X=""1"" Y=""1"" />
    </PointInfo>
    <PointInfo PName=""Q2"">
      <ThePoint X=""-1"" Y=""1"" />
    </PointInfo>
  </FamousPoints>
  <IntEnumerable yaxlib:realtype=""System.Collections.Generic.List`1[[System.Int32, {coreLibName}]]"">
    <Int32>1</Int32>
    <Int32>3</Int32>
    <Int32>5</Int32>
    <Int32>7</Int32>
  </IntEnumerable>
  <Students>
    <Count>3</Count>
    <Names>
      <String>Ali</String>
      <String>Dave</String>
      <String>John</String>
    </Names>
    <Families>
      <String>Alavi</String>
      <String>Black</String>
      <String>Doe</String>
    </Families>
  </Students>
</MoreComplexExample>";

            var serializer = new YAXSerializer(typeof(MoreComplexExample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(MoreComplexExample.GetSampleInstance());

            Assert.That(got.StripTypeAssemblyVersion(), Is.EqualTo(result.StripTypeAssemblyVersion()));
        }

        [Test]
        public void NestedDicSampleTest()
        {
            const string result =
                @"<!-- This example demonstrates usage of recursive collection serialization -->
<!-- and deserialization. In this case a Dictionary whose Key, or Value is -->
<!-- another dictionary or collection has been used. -->
<NestedDicSample>
  <SomeDic>
    <KeyValuePairOfDictionaryOfDoubleDictionaryOfInt32Int32DictionaryOfDictionaryOfStringStringListOfDouble>
      <Key>
        <KeyValuePairOfDoubleDictionaryOfInt32Int32>
          <Key>0.99999</Key>
          <Value>
            <KeyValuePairOfInt32Int32>
              <Key>1</Key>
              <Value>1</Value>
            </KeyValuePairOfInt32Int32>
            <KeyValuePairOfInt32Int32>
              <Key>2</Key>
              <Value>2</Value>
            </KeyValuePairOfInt32Int32>
            <KeyValuePairOfInt32Int32>
              <Key>3</Key>
              <Value>3</Value>
            </KeyValuePairOfInt32Int32>
            <KeyValuePairOfInt32Int32>
              <Key>4</Key>
              <Value>4</Value>
            </KeyValuePairOfInt32Int32>
          </Value>
        </KeyValuePairOfDoubleDictionaryOfInt32Int32>
        <KeyValuePairOfDoubleDictionaryOfInt32Int32>
          <Key>3.14</Key>
          <Value>
            <KeyValuePairOfInt32Int32>
              <Key>9</Key>
              <Value>1</Value>
            </KeyValuePairOfInt32Int32>
            <KeyValuePairOfInt32Int32>
              <Key>8</Key>
              <Value>2</Value>
            </KeyValuePairOfInt32Int32>
          </Value>
        </KeyValuePairOfDoubleDictionaryOfInt32Int32>
      </Key>
      <Value>
        <KeyValuePairOfDictionaryOfStringStringListOfDouble>
          <Key>
            <KeyValuePairOfStringString>
              <Key>Test</Key>
              <Value>123</Value>
            </KeyValuePairOfStringString>
            <KeyValuePairOfStringString>
              <Key>Test2</Key>
              <Value>456</Value>
            </KeyValuePairOfStringString>
          </Key>
          <Value>
            <Double>0.98767</Double>
            <Double>232</Double>
            <Double>13.124</Double>
          </Value>
        </KeyValuePairOfDictionaryOfStringStringListOfDouble>
        <KeyValuePairOfDictionaryOfStringStringListOfDouble>
          <Key>
            <KeyValuePairOfStringString>
              <Key>Num1</Key>
              <Value>123</Value>
            </KeyValuePairOfStringString>
            <KeyValuePairOfStringString>
              <Key>Num2</Key>
              <Value>456</Value>
            </KeyValuePairOfStringString>
          </Key>
          <Value>
            <Double>9.8767</Double>
            <Double>23.2</Double>
            <Double>1.34</Double>
          </Value>
        </KeyValuePairOfDictionaryOfStringStringListOfDouble>
      </Value>
    </KeyValuePairOfDictionaryOfDoubleDictionaryOfInt32Int32DictionaryOfDictionaryOfStringStringListOfDouble>
  </SomeDic>
</NestedDicSample>";
            var serializer = new YAXSerializer(typeof(NestedDicSample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(NestedDicSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void GuidDemoTest()
        {
            var g1 = Guid.NewGuid();
            var g2 = Guid.NewGuid();
            var g3 = Guid.NewGuid();
            var g4 = Guid.NewGuid();

            var result = string.Format(
                @"<!-- This example shows serialization and deserialization of GUID obejcts -->
<GUIDTest>
  <StandaloneGuid>{3}</StandaloneGuid>
  <SomeDic>
    <KeyValuePairOfGuidInt32>
      <Key>{0}</Key>
      <Value>1</Value>
    </KeyValuePairOfGuidInt32>
    <KeyValuePairOfGuidInt32>
      <Key>{1}</Key>
      <Value>2</Value>
    </KeyValuePairOfGuidInt32>
    <KeyValuePairOfGuidInt32>
      <Key>{2}</Key>
      <Value>3</Value>
    </KeyValuePairOfGuidInt32>
  </SomeDic>
</GUIDTest>", g1.ToString(), g2.ToString(), g3.ToString(), g4.ToString());
            var serializer = new YAXSerializer(typeof(GUIDTest), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(GUIDTest.GetSampleInstance(g1, g2, g3, g4));
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void NullableTest()
        {
            const string result =
                @"<!-- This exmaple shows the usage of nullable fields -->
<NullableClass>
  <Title>Inside C#</Title>
  <PublishYear>2002</PublishYear>
  <PurchaseYear />
</NullableClass>";
            var serializer = new YAXSerializer(typeof(NullableClass), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(NullableClass.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void NullableWithAttributeTest()
        {
            const string result =
                @"<!-- This exmaple shows the usage of nullable fields with an attribute blocking specific one. -->
<NullableClassAttribute>
  <Title>Inside C#</Title>
  <PublishYear>2002</PublishYear>
</NullableClassAttribute>";
            var serializer = new YAXSerializer(typeof(NullableClassAttribute), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(NullableClassAttribute.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void NullableSample2_Serialize()
        {
            const string expected =
                @"<NullableSample2 Number=""10"">
  <DateTime>1980-04-11T13:37:01.2345678Z</DateTime>
  <Decimal>1234.56789</Decimal>
  <Boolean>true</Boolean>
  <Enum>Autumn or fall</Enum>
</NullableSample2>";
            var serializer = new YAXSerializer(typeof(NullableSample2),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.SerializeNullObjects
                });
            var original = NullableSample2.GetSampleInstance();
            var got = serializer.Serialize(original);

            // Assert
            got.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void NullableSample2_Deserialize()
        {
            const string xml =
                @"<NullableSample2 Number=""10"">
  <DateTime>1980-04-11T13:37:01.2345678Z</DateTime>
  <Decimal>1234.56789</Decimal>
  <Boolean>true</Boolean>
  <Enum>Autumn or fall</Enum>
</NullableSample2>";
            var serializer = new YAXSerializer(typeof(NullableSample2),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.SerializeNullObjects
                });
            var original = NullableSample2.GetSampleInstance();
            var des = (NullableSample2) serializer.Deserialize(xml);

            // Assert
            des.Should().BeEquivalentTo(original);
        }

        [Test]
        public void NullableSample2WithNullAttribute_Serialize()
        {
            const string expected =
                @"<NullableSample2 Number=""10"">
  <DateTime>1980-04-11T13:37:01.2345678Z</DateTime>
  <Decimal>1234.56789</Decimal>
  <Boolean>true</Boolean>
  <Enum>Autumn or fall</Enum>
</NullableSample2>";
            var serializer = new YAXSerializer(typeof(NullableSample2),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.DontSerializeNullObjects
                });

            var original = NullableSample2.GetSampleInstance();
            
            var got = serializer.Serialize(original);

            // Assert
            got.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void NullableSample2WithNullAttribute_Deserialize()
        {
            const string xml =
                @"<NullableSample2 Number=""10"">
  <DateTime>1980-04-11T13:37:01.2345678Z</DateTime>
  <Decimal>1234.56789</Decimal>
  <Boolean>true</Boolean>
  <Enum>Autumn or fall</Enum>
</NullableSample2>";
            var serializer = new YAXSerializer(typeof(NullableSample2),
                new SerializerOptions {
                    ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.DoNotThrow,
                    ExceptionBehavior = YAXExceptionTypes.Warning,
                    SerializationOptions = YAXSerializationOptions.DontSerializeNullObjects
                });

            var original = NullableSample2.GetSampleInstance();
            var des = (NullableSample2) serializer.Deserialize(xml);

            // Assert
            des.Should().BeEquivalentTo(original);
        }

        [Test]
        public void NullableSample1WithNullAttribute_Serialize()
        {
            const string expected =
                @"<NullableSample1>
  <Text>Hello World</Text>
  <TestEnumProperty>yax-enum-for-EnumOne</TestEnumProperty>
  <TestEnumNullableProperty>yax-enum-for-EnumTwo</TestEnumNullableProperty>
  <TestEnumField>yax-enum-for-EnumOne</TestEnumField>
  <TestEnumNullableField>yax-enum-for-EnumThree</TestEnumNullableField>
</NullableSample1>";
            var original = NullableSample1.GetSampleInstance();
            var serializer = new YAXSerializer(typeof(NullableSample1));
            var got = serializer.Serialize(original);

            // Assert
            got.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void NullableSample1WithNullAttribute_Deserialize()
        {
            const string xml =
                @"<NullableSample1>
  <Text>Hello World</Text>
  <TestEnumProperty>yax-enum-for-EnumOne</TestEnumProperty>
  <TestEnumNullableProperty>yax-enum-for-EnumTwo</TestEnumNullableProperty>
  <TestEnumField>yax-enum-for-EnumOne</TestEnumField>
  <TestEnumNullableField>yax-enum-for-EnumThree</TestEnumNullableField>
</NullableSample1>";
            var original = NullableSample1.GetSampleInstance();
            var serializer = new YAXSerializer(typeof(NullableSample1));
            var des = (NullableSample1) serializer.Deserialize(xml);

            // Assert
            des.Should().BeEquivalentTo(original);
        }

        [Test]
        public void ListHolderClassTest()
        {
            const string result =
                @"<ListHolderClass>
  <ListOfStrings>
    <String>Hi</String>
    <String>Hello</String>
  </ListOfStrings>
</ListHolderClass>";
            var serializer = new YAXSerializer(typeof(ListHolderClass), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(ListHolderClass.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void StandaloneListTest()
        {
            const string result =
                @"<ListOfString>
  <String>Hi</String>
  <String>Hello</String>
</ListOfString>";
            var serializer = new YAXSerializer(ListHolderClass.GetSampleInstance().ListOfStrings.GetType(),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(ListHolderClass.GetSampleInstance().ListOfStrings);
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void NamesExampleTest()
        {
            const string result =
                @"<NamesExample>
  <FirstName>Li</FirstName>
  <Persons>
    <PersonInfo>
      <FirstName>Li</FirstName>
      <LastName />
    </PersonInfo>
    <PersonInfo>
      <FirstName>Hu</FirstName>
      <LastName>Hu</LastName>
    </PersonInfo>
  </Persons>
</NamesExample>";
            var serializer = new YAXSerializer(typeof(NamesExample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(NamesExample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void RequestTest()
        {
            const string result =
                @"<Pricing id=""123"">
  <version major=""1"" minor=""0"" />
  <input>
    <value_date>2010-10-5</value_date>
    <storage_date>2010-10-5</storage_date>
    <user>me</user>
    <skylab_config>
      <SomeString>someconf</SomeString>
      <job>test</job>
    </skylab_config>
  </input>
</Pricing>";
            var serializer = new YAXSerializer(typeof(Request), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(Request.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void AudioSampleTest()
        {
            const string result =
                @"<AudioSample>
  <Audio FileName=""filesname.jpg"">base64</Audio>
  <Image FileName=""filesname.jpg"">base64</Image>
</AudioSample>";
            var serializer = new YAXSerializer(typeof(AudioSample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(AudioSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void TimeSpanTest()
        {
            const string result =
                @"<!-- This example shows serialization and deserialization of TimeSpan obejcts -->
<TimeSpanSample>
  <TheTimeSpan>2.03:45:02.3000000</TheTimeSpan>
  <AnotherTimeSpan>2.03:45:02.3000000</AnotherTimeSpan>
  <DicTimeSpans>
    <KeyValuePairOfTimeSpanInt32>
      <Key>2.03:45:02.3000000</Key>
      <Value>1</Value>
    </KeyValuePairOfTimeSpanInt32>
    <KeyValuePairOfTimeSpanInt32>
      <Key>3.01:40:01.2000000</Key>
      <Value>2</Value>
    </KeyValuePairOfTimeSpanInt32>
  </DicTimeSpans>
</TimeSpanSample>";
            var serializer = new YAXSerializer(typeof(TimeSpanSample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(TimeSpanSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }


        [Test]
        public void FieldSerializationSampleTest()
        {
            const string result =
                @"<!-- This example shows how to choose the fields to be serialized -->
<FieldSerializationExample>
  <SomePrivateStringProperty>Hi</SomePrivateStringProperty>
  <_someInt>8</_someInt>
  <_someDouble>3.14</_someDouble>
</FieldSerializationExample>";
            var serializer = new YAXSerializer(typeof(FieldSerializationExample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(FieldSerializationExample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void MoreComplexBookTest()
        {
            const string result =
                @"<!-- This example shows how to provide serialization address -->
<!-- for elements and attributes. Theses addresses resemble those used -->
<!-- in known file-systems -->
<MoreComplexBook>
  <SomeTag>
    <SomeOtherTag>
      <AndSo Title=""Inside C#"">
        <Author>Tom Archer &amp; Andrew Whitechapel</Author>
      </AndSo>
    </SomeOtherTag>
  </SomeTag>
  <PublishYear>2002</PublishYear>
  <Price>30.5</Price>
</MoreComplexBook>";
            var serializer = new YAXSerializer(typeof(MoreComplexBook), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(MoreComplexBook.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void MoreComplexBookTwoTest()
        {
            const string result =
                @"<!-- This class shows how members of nested objects -->
<!-- can be serialized in their parents using serialization -->
<!-- addresses including "".."" -->
<MoreComplexBook2 Author_s_Name=""Tom Archer"">
  <Title>Inside C#</Title>
  <Something>
    <Or>
      <Another>
        <Author_s_Age>30</Author_s_Age>
      </Another>
    </Or>
  </Something>
  <PublishYear>2002</PublishYear>
  <Price>30.5</Price>
</MoreComplexBook2>";
            var serializer = new YAXSerializer(typeof(MoreComplexBook2), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(MoreComplexBook2.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void MoreComplexBookThreeTest()
        {
            const string result =
                @"<!-- This example shows how to serialize collection objects while -->
<!-- not serializing the element for their enclosing collection itself -->
<MoreComplexBook3>
  <Title>Inside C#</Title>
  <!-- Comment for author -->
  <PublishYear AuthorName=""Tom Archer"">2002</PublishYear>
  <AuthorAge>30</AuthorAge>
  <Price>30.5</Price>
  <Editor>Mark Twain</Editor>
  <Editor>Timothy Jones</Editor>
  <Editor>Oliver Twist</Editor>
</MoreComplexBook3>";
            var serializer = new YAXSerializer(typeof(MoreComplexBook3), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(MoreComplexBook3.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void WarehouseWithDictionaryNoContainerTest()
        {
            const string result =
                @"<!-- This example shows how dictionary objects can be serialized without -->
<!-- their enclosing element -->
<WarehouseWithDictionaryNoContainer Name=""Foo Warehousing Ltd."">
  <SiteInfo address=""No. 10, Some Ave., Some City, Some Country"">
    <SurfaceArea>120000.5</SurfaceArea>
  </SiteInfo>
  <StoreableItems>Item3, Item6, Item9, Item12</StoreableItems>
  <ItemInfo Item=""Item3"" Count=""10"" />
  <ItemInfo Item=""Item6"" Count=""120"" />
  <ItemInfo Item=""Item9"" Count=""600"" />
  <ItemInfo Item=""Item12"" Count=""25"" />
</WarehouseWithDictionaryNoContainer>";
            var serializer = new YAXSerializer(typeof(WarehouseWithDictionaryNoContainer),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseWithDictionaryNoContainer.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void WarehouseWithCommentsTest()
        {
            const string result =
                @"<WarehouseWithComments>
  <foo>
    <bar>
      <one>
        <two>
          <!-- Comment for name -->
          <Name>Foo Warehousing Ltd.</Name>
        </two>
        <!-- Comment for OwnerName -->
        <OwnerName>John Doe</OwnerName>
      </one>
    </bar>
  </foo>
  <SiteInfo address=""No. 10, Some Ave., Some City, Some Country"">
    <SurfaceArea>120000.5</SurfaceArea>
  </SiteInfo>
  <StoreableItems>Item3, Item6, Item9, Item12</StoreableItems>
  <!-- This dictionary is serilaized without container -->
  <ItemInfo Item=""Item3"" Count=""10"" />
  <ItemInfo Item=""Item6"" Count=""120"" />
  <ItemInfo Item=""Item9"" Count=""600"" />
  <ItemInfo Item=""Item12"" Count=""25"" />
</WarehouseWithComments>";
            var serializer = new YAXSerializer(typeof(WarehouseWithComments), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(WarehouseWithComments.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void EnumsSampleTest()
        {
            const string result =
                @"<!-- This example shows how to define aliases for enum members -->
<EnumsSample OneInstance=""Spring, Summer"">
  <TheSeasonSerially>Spring;Summer;Autumn or fall;Winter</TheSeasonSerially>
  <TheSeasonRecursive>
    <Seasons>Spring</Seasons>
    <Seasons>Summer</Seasons>
    <Seasons>Autumn or fall</Seasons>
    <Seasons>Winter</Seasons>
  </TheSeasonRecursive>
  <DicSeasonToInt>
    <KeyValuePairOfSeasonsInt32>
      <Key>Spring</Key>
      <Value>1</Value>
    </KeyValuePairOfSeasonsInt32>
    <KeyValuePairOfSeasonsInt32>
      <Key>Summer</Key>
      <Value>2</Value>
    </KeyValuePairOfSeasonsInt32>
    <KeyValuePairOfSeasonsInt32>
      <Key>Autumn or fall</Key>
      <Value>3</Value>
    </KeyValuePairOfSeasonsInt32>
    <KeyValuePairOfSeasonsInt32>
      <Key>Winter</Key>
      <Value>4</Value>
    </KeyValuePairOfSeasonsInt32>
  </DicSeasonToInt>
  <DicIntToSeason>
    <KeyValuePairOfInt32Seasons>
      <Key>1</Key>
      <Value>Spring</Value>
    </KeyValuePairOfInt32Seasons>
    <KeyValuePairOfInt32Seasons>
      <Key>2</Key>
      <Value>Spring, Summer</Value>
    </KeyValuePairOfInt32Seasons>
    <KeyValuePairOfInt32Seasons>
      <Key>3</Key>
      <Value>Autumn or fall</Value>
    </KeyValuePairOfInt32Seasons>
    <KeyValuePairOfInt32Seasons>
      <Key>4</Key>
      <Value>Winter</Value>
    </KeyValuePairOfInt32Seasons>
  </DicIntToSeason>
</EnumsSample>";
            var serializer = new YAXSerializer(typeof(EnumsSample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(EnumsSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void MultiDimArraySampleTest()
        {
            // Note: Double values must contain the round-trip value from
            // <double>.ToString("R", CultureInfo.InvariantCulture)
            // Otherwise the test, which is comparing strings, will fail
            var result =
                $@"<!-- This example shows serialization of multi-dimensional, -->
<!-- and jagged arrays -->
<MultiDimArraySample xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <IntArray yaxlib:dims=""2,3"">
    <Int32>1</Int32>
    <Int32>2</Int32>
    <Int32>3</Int32>
    <Int32>2</Int32>
    <Int32>3</Int32>
    <Int32>4</Int32>
  </IntArray>
  <DoubleArray yaxlib:dims=""2,3,3"">
    <Double>2</Double>
    <Double>{0.6666666666666666d.ToString("R", CultureInfo.InvariantCulture)}</Double>
    <Double>0.4</Double>
    <Double>2</Double>
    <Double>{0.6666666666666666d.ToString("R", CultureInfo.InvariantCulture)}</Double>
    <Double>0.4</Double>
    <Double>2</Double>
    <Double>{0.6666666666666666d.ToString("R", CultureInfo.InvariantCulture)}</Double>
    <Double>0.4</Double>
    <Double>2</Double>
    <Double>{0.6666666666666666d.ToString("R", CultureInfo.InvariantCulture)}</Double>
    <Double>0.4</Double>
    <Double>4</Double>
    <Double>{1.3333333333333333d.ToString("R", CultureInfo.InvariantCulture)}</Double>
    <Double>0.8</Double>
    <Double>6</Double>
    <Double>2</Double>
    <Double>1.2</Double>
  </DoubleArray>
  <JaggedArray>
    <Array1OfInt32>
      <Int32>1</Int32>
      <Int32>2</Int32>
    </Array1OfInt32>
    <Array1OfInt32>
      <Int32>1</Int32>
      <Int32>2</Int32>
      <Int32>3</Int32>
      <Int32>4</Int32>
    </Array1OfInt32>
    <Array1OfInt32>
      <Int32>1</Int32>
      <Int32>2</Int32>
      <Int32>3</Int32>
      <Int32>4</Int32>
      <Int32>5</Int32>
      <Int32>6</Int32>
    </Array1OfInt32>
  </JaggedArray>
  <!-- The containing element should not disappear because of the dims attribute -->
  <IntArrayNoContainingElems yaxlib:dims=""2,3"">
    <Int32>1</Int32>
    <Int32>2</Int32>
    <Int32>3</Int32>
    <Int32>2</Int32>
    <Int32>3</Int32>
    <Int32>4</Int32>
  </IntArrayNoContainingElems>
  <!-- This element should not be serialized serially because each element is not of basic type -->
  <JaggedNotSerially>
    <Array1OfInt32>
      <Int32>1</Int32>
      <Int32>2</Int32>
    </Array1OfInt32>
    <Array1OfInt32>
      <Int32>1</Int32>
      <Int32>2</Int32>
      <Int32>3</Int32>
      <Int32>4</Int32>
    </Array1OfInt32>
    <Array1OfInt32>
      <Int32>1</Int32>
      <Int32>2</Int32>
      <Int32>3</Int32>
      <Int32>4</Int32>
      <Int32>5</Int32>
      <Int32>6</Int32>
    </Array1OfInt32>
  </JaggedNotSerially>
</MultiDimArraySample>";
            var serializer = new YAXSerializer(typeof(MultiDimArraySample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);

            var got = serializer.Serialize(MultiDimArraySample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void AnotherArraySampleTest()
        {
            const string result =
                @"<!-- This example shows usage of jagged multi-dimensional arrays -->
<AnotherArraySample xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <Array1>
    <Array2OfInt32 yaxlib:dims=""2,3"">
      <Int32>1</Int32>
      <Int32>1</Int32>
      <Int32>1</Int32>
      <Int32>1</Int32>
      <Int32>2</Int32>
      <Int32>3</Int32>
    </Array2OfInt32>
    <Array2OfInt32 yaxlib:dims=""3,2"">
      <Int32>3</Int32>
      <Int32>3</Int32>
      <Int32>3</Int32>
      <Int32>4</Int32>
      <Int32>3</Int32>
      <Int32>5</Int32>
    </Array2OfInt32>
  </Array1>
</AnotherArraySample>";
            var serializer = new YAXSerializer(typeof(AnotherArraySample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(AnotherArraySample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }


        [Test]
        public void CollectionOfInterfacesSampleTest()
        {
            const string result =
                @"<!-- This example shows serialization and deserialization of -->
<!-- objects through a reference to their base class or interface -->
<CollectionOfInterfacesSample xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <SingleRef yaxlib:realtype=""YAXLibTests.SampleClasses.Class2"">
    <IntInInterface>22</IntInInterface>
    <StringInClass2>SingleRef</StringInClass2>
  </SingleRef>
  <ListOfSamples>
    <ISample yaxlib:realtype=""YAXLibTests.SampleClasses.Class1"">
      <IntInInterface>1</IntInInterface>
      <DoubleInClass1>1</DoubleInClass1>
    </ISample>
    <ISample yaxlib:realtype=""YAXLibTests.SampleClasses.Class2"">
      <IntInInterface>2</IntInInterface>
      <StringInClass2>Class2</StringInClass2>
    </ISample>
    <ISample yaxlib:realtype=""YAXLibTests.SampleClasses.Class3_1"">
      <StringInClass3_1>Class3_1</StringInClass3_1>
      <IntInInterface>3</IntInInterface>
      <DoubleInClass1>3</DoubleInClass1>
    </ISample>
  </ListOfSamples>
  <DictSample2Int>
    <KeyValuePairOfISampleInt32>
      <Key yaxlib:realtype=""YAXLibTests.SampleClasses.Class1"">
        <IntInInterface>1</IntInInterface>
        <DoubleInClass1>1</DoubleInClass1>
      </Key>
      <Value>1</Value>
    </KeyValuePairOfISampleInt32>
    <KeyValuePairOfISampleInt32>
      <Key yaxlib:realtype=""YAXLibTests.SampleClasses.Class2"">
        <IntInInterface>2</IntInInterface>
        <StringInClass2>Class2</StringInClass2>
      </Key>
      <Value>2</Value>
    </KeyValuePairOfISampleInt32>
    <KeyValuePairOfISampleInt32>
      <Key yaxlib:realtype=""YAXLibTests.SampleClasses.Class3_1"">
        <StringInClass3_1>Class3_1</StringInClass3_1>
        <IntInInterface>3</IntInInterface>
        <DoubleInClass1>3</DoubleInClass1>
      </Key>
      <Value>3</Value>
    </KeyValuePairOfISampleInt32>
  </DictSample2Int>
  <DictInt2Sample>
    <KeyValuePairOfInt32ISample>
      <Key>1</Key>
      <Value yaxlib:realtype=""YAXLibTests.SampleClasses.Class1"">
        <IntInInterface>1</IntInInterface>
        <DoubleInClass1>1</DoubleInClass1>
      </Value>
    </KeyValuePairOfInt32ISample>
    <KeyValuePairOfInt32ISample>
      <Key>2</Key>
      <Value yaxlib:realtype=""YAXLibTests.SampleClasses.Class2"">
        <IntInInterface>2</IntInInterface>
        <StringInClass2>Class2</StringInClass2>
      </Value>
    </KeyValuePairOfInt32ISample>
    <KeyValuePairOfInt32ISample>
      <Key>3</Key>
      <Value yaxlib:realtype=""YAXLibTests.SampleClasses.Class3_1"">
        <StringInClass3_1>Class3_1</StringInClass3_1>
        <IntInInterface>3</IntInInterface>
        <DoubleInClass1>3</DoubleInClass1>
      </Value>
    </KeyValuePairOfInt32ISample>
  </DictInt2Sample>
</CollectionOfInterfacesSample>";
            var serializer = new YAXSerializer(typeof(CollectionOfInterfacesSample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(CollectionOfInterfacesSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void MultipleCommentsTestTest()
        {
            const string result =
                @"<!-- How multi-line comments are serialized as multiple XML comments -->
<MultipleCommentsTest>
  <!-- Using @ quoted style -->
  <!-- comments for multiline comments -->
  <Dummy>0</Dummy>
  <!-- Comment 1 for member -->
  <!-- Comment 2 for member -->
  <SomeInt>10</SomeInt>
</MultipleCommentsTest>";
            var serializer = new YAXSerializer(typeof(MultipleCommentsTest), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(MultipleCommentsTest.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void InterfaceMatchingSampleTest()
        {
            const string result =
                @"<!-- This example shows serialization and deserialization of objects -->
<!-- through a reference to their base class or interface while used in -->
<!-- collection classes -->
<InterfaceMatchingSample SomeNumber=""10"">
  <ListOfSamples>2 4 8</ListOfSamples>
  <DictNullable2Int>
    <KeyValuePairOfNullableOfDoubleInt32 Key=""1"" Value=""1"" />
    <KeyValuePairOfNullableOfDoubleInt32 Key=""2"" Value=""2"" />
    <KeyValuePairOfNullableOfDoubleInt32 Key=""3"" Value=""3"" />
  </DictNullable2Int>
  <DictInt2Nullable>
    <KeyValuePairOfInt32NullableOfDouble Key=""1"" Value=""1"" />
    <KeyValuePairOfInt32NullableOfDouble Key=""2"" Value=""2"" />
    <KeyValuePairOfInt32NullableOfDouble Key=""3"" Value="""" />
  </DictInt2Nullable>
</InterfaceMatchingSample>";
            var serializer = new YAXSerializer(typeof(InterfaceMatchingSample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(InterfaceMatchingSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void NonGenericCollectionsSampleTest()
        {
            const string result =
                @"<!-- This sample demonstrates serialization of non-generic collection classes -->
<NonGenericCollectionsSample xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <ObjList Author_s_Name=""Charles"">
    <Object yaxlib:realtype=""System.Int32"">1</Object>
    <Object yaxlib:realtype=""System.Double"">3</Object>
    <Object yaxlib:realtype=""System.String"">Hello</Object>
    <Object yaxlib:realtype=""System.DateTime"">03/04/2010 00:00:00</Object>
    <Something>
      <Or>
        <Another>
          <Author_s_Age>50</Author_s_Age>
        </Another>
      </Or>
    </Something>
    <Object yaxlib:realtype=""YAXLibTests.SampleClasses.Author"" />
  </ObjList>
  <TheArrayList Author_s_Name=""Steve"">
    <Object yaxlib:realtype=""System.Int32"">2</Object>
    <Object yaxlib:realtype=""System.Double"">8.5</Object>
    <Object yaxlib:realtype=""System.String"">Hi</Object>
    <Something>
      <Or>
        <Another>
          <Author_s_Age>30</Author_s_Age>
        </Another>
      </Or>
    </Something>
    <Object yaxlib:realtype=""YAXLibTests.SampleClasses.Author"" />
  </TheArrayList>
  <TheHashtable>
{0}
{1}
{2}
  </TheHashtable>
  <TheQueue>
    <Object yaxlib:realtype=""System.Int32"">10</Object>
    <Object yaxlib:realtype=""System.Int32"">20</Object>
    <Object yaxlib:realtype=""System.Int32"">30</Object>
  </TheQueue>
  <TheStack>
    <Object yaxlib:realtype=""System.Int32"">300</Object>
    <Object yaxlib:realtype=""System.Int32"">200</Object>
    <Object yaxlib:realtype=""System.Int32"">100</Object>
  </TheStack>
  <TheSortedList>
    <Object yaxlib:realtype=""System.Collections.DictionaryEntry"">
      <Key yaxlib:realtype=""System.Int32"">1</Key>
      <Value yaxlib:realtype=""System.Int32"">2</Value>
    </Object>
    <Object yaxlib:realtype=""System.Collections.DictionaryEntry"">
      <Key yaxlib:realtype=""System.Int32"">5</Key>
      <Value yaxlib:realtype=""System.Int32"">7</Value>
    </Object>
    <Object yaxlib:realtype=""System.Collections.DictionaryEntry"">
      <Key yaxlib:realtype=""System.Int32"">8</Key>
      <Value yaxlib:realtype=""System.Int32"">2</Value>
    </Object>
  </TheSortedList>
  <TheBitArray>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">true</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">true</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
    <Object yaxlib:realtype=""System.Boolean"">false</Object>
  </TheBitArray>
</NonGenericCollectionsSample>";

            var part1 = @"    <Object yaxlib:realtype=""System.Collections.DictionaryEntry"">
      <Key yaxlib:realtype=""System.DateTime"">02/01/2009 00:00:00</Key>
      <Value yaxlib:realtype=""System.Int32"">7</Value>
    </Object>";

            var part2 = @"    <Object yaxlib:realtype=""System.Collections.DictionaryEntry"">
      <Key yaxlib:realtype=""System.String"">Tom</Key>
      <Value yaxlib:realtype=""System.String"">Sam</Value>
    </Object>";
            var part3 = @"    <Object yaxlib:realtype=""System.Collections.DictionaryEntry"">
      <Key yaxlib:realtype=""System.Double"">1</Key>
      <Value yaxlib:realtype=""System.String"">Tim</Value>
    </Object>";

            var possibleResult1 = string.Format(result, part1, part2, part3);
            var possibleResult2 = string.Format(result, part1, part3, part2);
            var possibleResult3 = string.Format(result, part2, part1, part3);
            var possibleResult4 = string.Format(result, part2, part3, part1);
            var possibleResult5 = string.Format(result, part3, part1, part2);
            var possibleResult6 = string.Format(result, part3, part2, part1);

            var serializer = new YAXSerializer(typeof(NonGenericCollectionsSample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(NonGenericCollectionsSample.GetSampleInstance());
            //result.ShouldEqualWithDiff(got, DiffStyle.Minimal);

            var result1Match = string.Equals(got, possibleResult1, StringComparison.Ordinal);
            var result2Match = string.Equals(got, possibleResult2, StringComparison.Ordinal);
            var result3Match = string.Equals(got, possibleResult3, StringComparison.Ordinal);
            var result4Match = string.Equals(got, possibleResult4, StringComparison.Ordinal);
            var result5Match = string.Equals(got, possibleResult5, StringComparison.Ordinal);
            var result6Match = string.Equals(got, possibleResult6, StringComparison.Ordinal);

            Assert.That(result1Match || result2Match || result3Match || result4Match || result5Match || result6Match,
                Is.True);
        }


        [Test]
        public void GenericCollectionsSampleTest()
        {
            const string result =
                @"<!-- This class provides an example of successful serialization/deserialization -->
<!-- of collection objects in ""System.Collections.Generic"" namespaces -->
<GenericCollectionsSample>
  <TheStack>
    <Int32>79</Int32>
    <Int32>1</Int32>
    <Int32>7</Int32>
  </TheStack>
  <TheSortedList>
    <Item Key=""0.5"" Value=""Hello"" />
    <Item Key=""1"" Value=""Hi"" />
    <Item Key=""5"" Value=""How are you?"" />
  </TheSortedList>
  <TheSortedDictionary>
    <Item Key=""1"" Value=""30"" />
    <Item Key=""5"" Value=""2"" />
    <Item Key=""10"" Value=""1"" />
  </TheSortedDictionary>
  <TheQueue>
    <String>Hi</String>
    <String>Hello</String>
    <String>How are you?</String>
  </TheQueue>
  <TheHashSet>
    <Int32>1</Int32>
    <Int32>2</Int32>
    <Int32>4</Int32>
    <Int32>6</Int32>
  </TheHashSet>
  <TheLinkedList>
    <Double>1</Double>
    <Double>5</Double>
    <Double>61</Double>
  </TheLinkedList>
</GenericCollectionsSample>";

            var serializer = new YAXSerializer(typeof(GenericCollectionsSample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(GenericCollectionsSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void SerializingPathAndAliasTogetherTest()
        {
            const string result =
                @"<PathAndAliasAssignmentSample>
  <Title value=""Inside C#"" />
  <Price value=""30.5"" />
  <Publish year=""2002"" />
  <Notes>
    <Comments value=""SomeComment"" />
  </Notes>
  <Author name=""Tom Archer &amp; Andrew Whitechapel"" />
</PathAndAliasAssignmentSample>";
            var serializer = new YAXSerializer(typeof(PathAndAliasAssignmentSample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(PathAndAliasAssignmentSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void CollectionSeriallyAsAttributeTest()
        {
            const string result =
                @"<CollectionSeriallyAsAttribute>
  <Info names=""John Doe,Jane,Sina,Mike,Rich"" />
  <TheCities>Tehran,Melbourne,New York,Paris</TheCities>
  <Location>
    <Countries>Iran,Australia,United States of America,France</Countries>
  </Location>
</CollectionSeriallyAsAttribute>";
            var serializer = new YAXSerializer(typeof(CollectionSeriallyAsAttribute),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(CollectionSeriallyAsAttribute.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }


        [Test]
        public void SerializationOptionsSampleTest()
        {
            const string resultWithSerializeNullRefs =
                @"<SerializationOptionsSample>
  <!-- Str2Null must NOT be serialized when it is null, even -->
  <!-- if the serialization options of the serializer is changed -->
  <ObjectWithOptionsSet>
    <StrNotNull>SomeString</StrNotNull>
    <SomeValueType>0</SomeValueType>
  </ObjectWithOptionsSet>
  <!-- Str2Null must be serialized when it is null, even -->
  <!-- if the serialization options of the serializer is changed -->
  <AnotherObjectWithOptionsSet>
    <StrNotNull>Some other string</StrNotNull>
    <StrNull />
  </AnotherObjectWithOptionsSet>
  <!-- serialization of Str2Null must obey the options set -->
  <!-- in the serializer itself -->
  <ObjectWithoutOptionsSet>
    <StrNotNull>Another string</StrNotNull>
    <StrNull />
  </ObjectWithoutOptionsSet>
</SerializationOptionsSample>";

            var serializer = new YAXSerializer(typeof(SerializationOptionsSample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(SerializationOptionsSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(resultWithSerializeNullRefs));

            const string resultWithDontSerializeNullRefs =
                @"<SerializationOptionsSample>
  <!-- Str2Null must NOT be serialized when it is null, even -->
  <!-- if the serialization options of the serializer is changed -->
  <ObjectWithOptionsSet>
    <StrNotNull>SomeString</StrNotNull>
    <SomeValueType>0</SomeValueType>
  </ObjectWithOptionsSet>
  <!-- Str2Null must be serialized when it is null, even -->
  <!-- if the serialization options of the serializer is changed -->
  <AnotherObjectWithOptionsSet>
    <StrNotNull>Some other string</StrNotNull>
    <StrNull />
  </AnotherObjectWithOptionsSet>
  <!-- serialization of Str2Null must obey the options set -->
  <!-- in the serializer itself -->
  <ObjectWithoutOptionsSet>
    <StrNotNull>Another string</StrNotNull>
  </ObjectWithoutOptionsSet>
</SerializationOptionsSample>";

            serializer = new YAXSerializer(typeof(SerializationOptionsSample), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.DontSerializeNullObjects);
            got = serializer.Serialize(SerializationOptionsSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(resultWithDontSerializeNullRefs));
        }

        [Test]
        public void SerializeAClassContainingXElementItself()
        {
            var initialInstance = ClassContainingXElement.GetSampleInstance();
            var initialInstanceString = initialInstance.ToString();

            var ser = new YAXSerializer(typeof(ClassContainingXElement), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);

            var initialXmlSer = ser.Serialize(initialInstance);

            var initialInstDes = ser.Deserialize(initialXmlSer) as ClassContainingXElement;
            Assert.That(initialInstDes, Is.Not.Null);
            var initialInstDesString = initialInstDes.ToString();

            Assert.That(initialInstDesString, Is.EqualTo(initialInstanceString));

            initialInstance.TheElement = null;
            var nulledElementString = initialInstance.ToString();

            var nulledElemXmlSer = ser.Serialize(initialInstance);

            var nulledInstanceDeser = ser.Deserialize(nulledElemXmlSer);
            Assert.That(nulledInstanceDeser.ToString(), Is.EqualTo(nulledElementString));
        }

        [Test]
        public void SerializaitonOfPropertylessClasses()
        {
            const string result =
                @"<PropertylessClassesSample xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <ValuedDbNull>DBNull</ValuedDbNull>
  <NullDbNull />
  <ObjValuedDbNull yaxlib:realtype=""System.DBNull"">DBNull</ObjValuedDbNull>
  <ObjNullDbNull />
  <ValuedRandom />
  <NullRandom />
  <ObjValuedRandom yaxlib:realtype=""System.Random"" />
  <ObjNullRandom />
</PropertylessClassesSample>";
            var serializer = new YAXSerializer(typeof(PropertylessClassesSample),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(PropertylessClassesSample.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void GuidsAsBasicTypeTest()
        {
            const string result =
                @"<GuidAsBasicType GuidAsAttr=""fed92f33-e351-47bd-9018-69c89928329e"">
  <GuidAsElem>042ba99c-b679-4975-ac4d-2fe563a5dc3e</GuidAsElem>
  <GuidArray>
    <Guid>fed92f33-e351-47bd-9018-69c89928329e</Guid>
    <Guid>042ba99c-b679-4975-ac4d-2fe563a5dc3e</Guid>
    <Guid>82071c51-ea20-473b-a541-1ebdf8f158d3</Guid>
    <Guid>81a3478b-5779-451a-b2aa-fbf69bb11424</Guid>
    <Guid>d626ba2b-a095-4a34-a376-997e5628dfb9</Guid>
  </GuidArray>
  <GuidArraySerially>fed92f33-e351-47bd-9018-69c89928329e 042ba99c-b679-4975-ac4d-2fe563a5dc3e 82071c51-ea20-473b-a541-1ebdf8f158d3 81a3478b-5779-451a-b2aa-fbf69bb11424 d626ba2b-a095-4a34-a376-997e5628dfb9</GuidArraySerially>
  <GuidsList>
    <Guid>fed92f33-e351-47bd-9018-69c89928329e</Guid>
    <Guid>042ba99c-b679-4975-ac4d-2fe563a5dc3e</Guid>
    <Guid>82071c51-ea20-473b-a541-1ebdf8f158d3</Guid>
    <Guid>81a3478b-5779-451a-b2aa-fbf69bb11424</Guid>
    <Guid>d626ba2b-a095-4a34-a376-997e5628dfb9</Guid>
  </GuidsList>
  <DicKeyGuid>
    <KeyValuePairOfGuidInt32>
      <Key>fed92f33-e351-47bd-9018-69c89928329e</Key>
      <Value>1</Value>
    </KeyValuePairOfGuidInt32>
    <KeyValuePairOfGuidInt32>
      <Key>042ba99c-b679-4975-ac4d-2fe563a5dc3e</Key>
      <Value>2</Value>
    </KeyValuePairOfGuidInt32>
    <KeyValuePairOfGuidInt32>
      <Key>82071c51-ea20-473b-a541-1ebdf8f158d3</Key>
      <Value>3</Value>
    </KeyValuePairOfGuidInt32>
  </DicKeyGuid>
  <DicKeyAttrGuid>
    <Pair TheGuid=""fed92f33-e351-47bd-9018-69c89928329e"">
      <Value>1</Value>
    </Pair>
    <Pair TheGuid=""042ba99c-b679-4975-ac4d-2fe563a5dc3e"">
      <Value>2</Value>
    </Pair>
    <Pair TheGuid=""82071c51-ea20-473b-a541-1ebdf8f158d3"">
      <Value>3</Value>
    </Pair>
  </DicKeyAttrGuid>
  <DicValueGuid>
    <KeyValuePairOfInt32Guid>
      <Key>1</Key>
      <Value>fed92f33-e351-47bd-9018-69c89928329e</Value>
    </KeyValuePairOfInt32Guid>
    <KeyValuePairOfInt32Guid>
      <Key>2</Key>
      <Value>82071c51-ea20-473b-a541-1ebdf8f158d3</Value>
    </KeyValuePairOfInt32Guid>
    <KeyValuePairOfInt32Guid>
      <Key>3</Key>
      <Value>d626ba2b-a095-4a34-a376-997e5628dfb9</Value>
    </KeyValuePairOfInt32Guid>
  </DicValueGuid>
  <DicValueAttrGuid>
    <Pair TheGuid=""fed92f33-e351-47bd-9018-69c89928329e"">
      <Key>1</Key>
    </Pair>
    <Pair TheGuid=""82071c51-ea20-473b-a541-1ebdf8f158d3"">
      <Key>2</Key>
    </Pair>
    <Pair TheGuid=""d626ba2b-a095-4a34-a376-997e5628dfb9"">
      <Key>3</Key>
    </Pair>
  </DicValueAttrGuid>
</GuidAsBasicType>";
            var serializer = new YAXSerializer(typeof(GuidAsBasicType), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(GuidAsBasicType.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void PolymorphicSerializationThroughObjectTest()
        {
            object content = "this is just a simple test";
            var ser = new YAXSerializer(typeof(object));
            var xmlResult = ser.Serialize(content);

            var expectedResult =
                @"<Object yaxlib:realtype=""System.String"" xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">this is just a simple test</Object>";

            Assert.That(xmlResult, Is.EqualTo(expectedResult));
            var desObj = ser.Deserialize(xmlResult);
            var objStr = desObj.ToString();
            Assert.That(desObj.ToString(), Is.EqualTo(content.ToString()));
        }

        [TestCase("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // NETFRAMEWORK2.x
        [TestCase("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // NETFRAMEWORK4.x
        [TestCase("System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")] // NETSTANDARD
        [TestCase("System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")] // NET5.0
        public void PolymorphicSerializationThroughList_CrossFramework_Test(string coreLibName)
        {
            var lst = new List<int> {1, 2, 3};
            var ser = new YAXSerializer(typeof(object));
            var xmlResult = ser.Serialize(lst);

            var expectedResult =
                $@"<Object yaxlib:realtype=""System.Collections.Generic.List`1[[System.Int32, {coreLibName}]]"" xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <Int32>1</Int32>
  <Int32>2</Int32>
  <Int32>3</Int32>
</Object>";

            Assert.That(xmlResult.StripTypeAssemblyVersion(), Is.EqualTo(expectedResult.StripTypeAssemblyVersion()));
            var desObj = ser.Deserialize(xmlResult);
            Assert.That(desObj.GetType(), Is.EqualTo(lst.GetType()));
            var desLst = desObj as List<int>;
            Assert.That(lst, Has.Count.EqualTo(desLst.Count));
            Assert.That(lst, Is.EquivalentTo(desLst));
        }

        [TestCase("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // NETFRAMEWORK2.x
        [TestCase("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // NETFRAMEWORK4.x
        [TestCase("System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")] // NETSTANDARD
        [TestCase("System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")] // NET5.0
        public void PolymorphicSerializationThroughListWhichMayContainYaxlibNamespace_CrossFramework_Test(string coreLibName)
        {
            var lst = new List<object> {1, 2, 3};
            var ser = new YAXSerializer(typeof(object));
            var xmlResult = ser.Serialize(lst);

            var expectedResult =
                $@"<Object xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"" yaxlib:realtype=""System.Collections.Generic.List`1[[System.Object, {coreLibName}]]"">
  <Object yaxlib:realtype=""System.Int32"">1</Object>
  <Object yaxlib:realtype=""System.Int32"">2</Object>
  <Object yaxlib:realtype=""System.Int32"">3</Object>
</Object>";

            Assert.That(xmlResult.StripTypeAssemblyVersion(), Is.EqualTo(expectedResult.StripTypeAssemblyVersion()));
            var desObj = ser.Deserialize(xmlResult);
            Assert.That(desObj.GetType(), Is.EqualTo(lst.GetType()));
            var desLst = desObj as List<object>;
            Assert.That(lst, Has.Count.EqualTo(desLst.Count));
            Assert.That(lst, Is.EquivalentTo(desLst));
        }

        [Test]
        public void DashPreservationTest()
        {
            const string expectedResult = @"<dashed-sample dashed-name=""Name"" />";

            var sample = new DashedSample
            {
                DashedName = "Name"
            };

            var ser = new YAXSerializer(typeof(DashedSample));
            var got = ser.Serialize(sample);
            Assert.That(got, Is.EqualTo(expectedResult));
        }

        [Test]
        public void AttributeForClassTest()
        {
            var ser = new YAXSerializer(typeof(AttributeContainerSample));
            var result = ser.Serialize(AttributeContainerSample.GetSampleInstance());

            const string expectedResult =
                @"<container>
  <range from=""1"" to=""3"" />
</container>";

            Assert.That(expectedResult, Is.EqualTo(result));
        }

        [Test]
        public void AttributeForSubclassTest()
        {
            var ser = new YAXSerializer(typeof(AttributeSubclassSample));
            var result = ser.Serialize(AttributeSubclassSample.GetSampleInstance());

            const string expectedResult = @"<subclass url=""http://example.com/subclass/1"" page=""1"" />";
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void DictionaryKeyValueAsContentTest()
        {
            var ser = new YAXSerializer(typeof(DictionaryKeyValueAsContent));
            var result = ser.Serialize(DictionaryKeyValueAsContent.GetSampleInstance());

            const string expectedResult =
                @"<DictionaryKeyValueAsContent>
  <DicValueAsContent>
    <Pair Digits=""1"">one</Pair>
    <Pair Digits=""2"">two</Pair>
    <Pair Digits=""3"">three</Pair>
  </DicValueAsContent>
  <DicKeyAsContnet>
    <Pair Letters=""one"">1</Pair>
    <Pair Letters=""two"">2</Pair>
    <Pair Letters=""three"">3</Pair>
  </DicKeyAsContnet>
  <DicKeyAsContentValueAsElement>
    <Pair>1<Letters>one</Letters></Pair>
    <Pair>2<Letters>two</Letters></Pair>
    <Pair>3<Letters>three</Letters></Pair>
  </DicKeyAsContentValueAsElement>
  <DicValueAsContentKeyAsElement>
    <Pair>
      <Digits>1</Digits>one</Pair>
    <Pair>
      <Digits>2</Digits>two</Pair>
    <Pair>
      <Digits>3</Digits>three</Pair>
  </DicValueAsContentKeyAsElement>
</DictionaryKeyValueAsContent>";
            Assert.That(expectedResult, Is.EqualTo(result));
        }

        [Test]
        public void AttributeForKeyInDictionaryTest()
        {
            var dictionary = DictionarySample.GetSampleInstance();
            var ser = new YAXSerializer(typeof(DictionarySample));
            var result = ser.Serialize(dictionary);

            const string expectedResult =
                @"<TheItems xmlns=""http://example.com/"">
  <TheItem TheKey=""key1"">00000001-0002-0003-0405-060708090a0b</TheItem>
  <TheItem TheKey=""key2"">1234</TheItem>
</TheItems>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void AttributeForKeyInDictionaryPropertyTest()
        {
            var container = DictionaryContainerSample.GetSampleInstance();
            var ser = new YAXSerializer(typeof(DictionaryContainerSample));
            var result = ser.Serialize(container);

            const string expectedResult =
                @"<container xmlns=""http://example.com/"">
  <items>
    <item key=""key1"">00000001-0002-0003-0405-060708090a0b</item>
    <item key=""key2"">1234</item>
  </items>
</container>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void CollectionWithExtraPropertiesTest()
        {
            var container = CollectionWithExtraProperties.GetSampleInstance();
            var ser = new YAXSerializer(typeof(CollectionWithExtraProperties));
            var result = ser.Serialize(container);

            const string expectedResult =
                @"<CollectionWithExtraProperties>
  <Property1>Property1</Property1>
  <Property2>1.234</Property2>
  <Int32>1</Int32>
  <Int32>2</Int32>
  <Int32>3</Int32>
  <Int32>4</Int32>
</CollectionWithExtraProperties>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void CollectionWithExtraPropertiesAttributedAsNotCollectionTest()
        {
            var container = CollectionWithExtraPropertiesAttributedAsNotCollection.GetSampleInstance();
            var ser = new YAXSerializer(typeof(CollectionWithExtraPropertiesAttributedAsNotCollection));
            var result = ser.Serialize(container);

            const string expectedResult =
                @"<CollectionWithExtraPropertiesAttributedAsNotCollection>
  <Property1>Property1</Property1>
  <Property2>1.234</Property2>
</CollectionWithExtraPropertiesAttributedAsNotCollection>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void DictionaryWithExtraPropertiesTest()
        {
            var container = DictionaryWithExtraProperties.GetSampleInstance();
            var ser = new YAXSerializer(typeof(DictionaryWithExtraProperties));
            var result = ser.Serialize(container);

            const string expectedResult =
                @"<DictionaryWithExtraProperties>
  <Prop1>Prop1</Prop1>
  <Prop2>2.234</Prop2>
  <Pair>
    <Key>1</Key>
    <Value>One</Value>
  </Pair>
  <Pair>
    <Key>2</Key>
    <Value>Two</Value>
  </Pair>
  <Pair>
    <Key>3</Key>
    <Value>Three</Value>
  </Pair>
</DictionaryWithExtraProperties>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void DictionaryWithExtraPropertiesAttributedAsNotCollectionTest()
        {
            var container = DictionaryWithExtraPropertiesAttributedAsNotCollection.GetSampleInstance();
            var ser = new YAXSerializer(typeof(DictionaryWithExtraPropertiesAttributedAsNotCollection));
            var result = ser.Serialize(container);

            const string expectedResult =
                @"<DictionaryWithExtraPropertiesAttributedAsNotCollection>
  <Prop1>Prop1</Prop1>
  <Prop2>2.234</Prop2>
</DictionaryWithExtraPropertiesAttributedAsNotCollection>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void AttributeInheritanceTest()
        {
            const string result =
                @"<Child>
  <TheAge>30.2</TheAge>
  <TheName>John</TheName>
  <TheGender>Unknown</TheGender>
</Child>";
            var serializer = new YAXSerializer(typeof(AttributeInheritance), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Warning, YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(AttributeInheritance.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void AttributeInheritanceWithPropertyOverrideTest()
        {
            const string result =
                @"<Child>
  <TheGender>Female</TheGender>
  <CurrentAge>38.7</CurrentAge>
  <TheName>Sally</TheName>
</Child>";
            var serializer = new YAXSerializer(typeof(AttributeInheritanceWithPropertyOverride),
                YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Warning,
                YAXSerializationOptions.SerializeNullObjects);
            var got = serializer.Serialize(AttributeInheritanceWithPropertyOverride.GetSampleInstance());
            Assert.That(got, Is.EqualTo(result));
        }

        [Test]
        public void ListOfPolymorphicObjectsTest()
        {
            var ser = new YAXSerializer(typeof(PolymorphicSampleList));
            var result = ser.Serialize(PolymorphicSampleList.GetSampleInstance());

            const string expectedResult =
                @"<samples xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <sample yaxlib:realtype=""YAXLibTests.SampleClasses.PolymorphicOneSample"" />
  <sample yaxlib:realtype=""YAXLibTests.SampleClasses.PolymorphicTwoSample"" />
</samples>";
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void OneLetterPathTest()
        {
            var ser = new YAXSerializer(typeof(OneLetterAlias));
            var result = ser.Serialize(OneLetterAlias.GetSampleInstance());

            const string expectedResult =
                @"<OneLetterAlias>
  <T>Inside C#</T>
  <A>Tom Archer &amp; Andrew Whitechapel</A>
</OneLetterAlias>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void IndexerPropertiesAreNotSerialized()
        {
            var ser = new YAXSerializer(typeof(IndexerSample));
            var result = ser.Serialize(IndexerSample.GetSampleInstance());

            const string expectedResult =
                @"<IndexerSample>
  <SomeInt>1234</SomeInt>
  <SomeString>Something</SomeString>
</IndexerSample>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void SingleLetterPropertyNamesAreSerializedProperly()
        {
            var ser = new YAXSerializer(typeof(SingleLetterPropertyNames));
            var result = ser.Serialize(SingleLetterPropertyNames.GetSampleInstance());

            const string expectedResult =
                @"<SingleLetterPropertyNames>
  <TestPoints>
    <TestPoint>
      <Id>0</Id>
      <X>100</X>
      <Y>100</Y>
    </TestPoint>
    <TestPoint>
      <Id>1</Id>
      <X>-100</X>
      <Y>150</Y>
    </TestPoint>
  </TestPoints>
</SingleLetterPropertyNames>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void DelegatesAndFunctionPointersMustBeIgnored()
        {
            var ser = new YAXSerializer(typeof(DelegateInstances));
            var result = ser.Serialize(DelegateInstances.GetSampleInstance());

            const string expectedResult =
                @"<DelegateInstances>
  <SomeNumber>12</SomeNumber>
</DelegateInstances>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void RepetitiveReferencesAreNotLoop()
        {
            var ser = new YAXSerializer(typeof(RepetitiveReferenceIsNotLoop));
            var result = ser.Serialize(RepetitiveReferenceIsNotLoop.GetSampleInstance());

            const string expectedResult =
                @"<RepetitiveReferenceIsNotLoop>
  <RefA>
    <N>10</N>
  </RefA>
  <RefB>
    <N>10</N>
  </RefB>
</RepetitiveReferenceIsNotLoop>";
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void SelfReferringTypeIsNotNecessarilyASelfReferringObject()
        {
            var ser = new YAXSerializer(typeof(DirectSelfReferringObject));
            var result = ser.Serialize(DirectSelfReferringObject.GetSampleInstance());

            const string expenctedResult =
                @"<DirectSelfReferringObject>
  <Data>1</Data>
  <Next>
    <Data>2</Data>
    <Next />
  </Next>
</DirectSelfReferringObject>";

            Assert.AreEqual(expenctedResult, result);
        }

        [Test]
        public void SerializingASelfReferringObjectThrowsException_WhenTheRelevantSerializationOptionIsSet()
        {
            Assert.Throws<YAXCannotSerializeSelfReferentialTypes>(() =>
            {
                var ser = new YAXSerializer(typeof(DirectSelfReferringObject),
                    YAXSerializationOptions.ThrowUponSerializingCyclingReferences);
                var result = ser.Serialize(DirectSelfReferringObject.GetSampleInstanceWithCycle());
            });
        }

        [Test]
        public void SerializingAnIndirectSelfReferringTypeWithougLoopMustPass()
        {
            var ser = new YAXSerializer(typeof(IndirectSelfReferringObject));
            var result = ser.Serialize(IndirectSelfReferringObject.GetSampleInstance());

            const string expectedResult =
                @"<IndirectSelfReferringObject>
  <ParentDescription>I'm Parent</ParentDescription>
  <Child>
    <ChildDescription>I'm Child</ChildDescription>
    <Parent />
  </Child>
</IndirectSelfReferringObject>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void SerializingAnIndirectSelfReferringObjectThrowsException_WhenTheRelevantOptionIsSet()
        {
            Assert.Throws<YAXCannotSerializeSelfReferentialTypes>(() =>
            {
                var ser = new YAXSerializer(typeof(IndirectSelfReferringObject),
                    YAXSerializationOptions.ThrowUponSerializingCyclingReferences);
                var result = ser.Serialize(IndirectSelfReferringObject.GetSampleInstanceWithLoop());
            });
        }


        [Test]
        public void
            SerializingAnIndirectSelfReferringObjectMustPassWhenThrowUponSerializingCyclingReferencesOptionIsNotSet()
        {
            var ser = new YAXSerializer(typeof(IndirectSelfReferringObject),
                YAXExceptionHandlingPolicies.ThrowWarningsAndErrors, YAXExceptionTypes.Error);
            var result = ser.Serialize(IndirectSelfReferringObject.GetSampleInstanceWithLoop());

            const string expectedResult =
                @"<IndirectSelfReferringObject>
  <ParentDescription>I'm Parent</ParentDescription>
  <Child>
    <ChildDescription>I'm Child</ChildDescription>
    <Parent />
  </Child>
</IndirectSelfReferringObject>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void
            SerializingAnIndirectSelfReferringObjectMustThrowWhenThrowUponSerializingCyclingReferencesOptionIsSet()
        {
            Assert.Throws<YAXCannotSerializeSelfReferentialTypes>(() =>
            {
                var ser = new YAXSerializer(typeof(IndirectSelfReferringObject),
                    YAXExceptionHandlingPolicies.ThrowWarningsAndErrors, YAXExceptionTypes.Error,
                    YAXSerializationOptions.ThrowUponSerializingCyclingReferences);
                var result = ser.Serialize(IndirectSelfReferringObject.GetSampleInstanceWithLoop());
            });
        }

        [Test]
        public void
            SerializingDirectSelfReferringObjectMustPassWhenThrowUponSerializingCyclingReferencesOptionIsNotSet()
        {
            var ser = new YAXSerializer(typeof(DirectSelfReferringObject),
                YAXExceptionHandlingPolicies.ThrowWarningsAndErrors, YAXExceptionTypes.Error);
            var result = ser.Serialize(DirectSelfReferringObject.GetSampleInstanceWithCycle());

            const string expectedResult =
                @"<DirectSelfReferringObject>
  <Data>1</Data>
  <Next>
    <Data>2</Data>
    <Next />
  </Next>
</DirectSelfReferringObject>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void SerializingDirectSelfReferringObjectMustThrowWhenThrowUponSerializingCyclingReferencesOptionIsSet()
        {
            Assert.Throws<YAXCannotSerializeSelfReferentialTypes>(() =>
            {
                var ser = new YAXSerializer(typeof(DirectSelfReferringObject),
                    YAXExceptionHandlingPolicies.ThrowWarningsAndErrors, YAXExceptionTypes.Error,
                    YAXSerializationOptions.ThrowUponSerializingCyclingReferences);
                var result = ser.Serialize(DirectSelfReferringObject.GetSampleInstanceWithCycle());
            });
        }

        [Test]
        public void
            SerializingDirectSelfReferringObjectWithSelfCycleMustPassWhenThrowUponSerializingCyclingReferencesOptionIsNotSet()
        {
            var ser = new YAXSerializer(typeof(DirectSelfReferringObject),
                YAXExceptionHandlingPolicies.ThrowWarningsAndErrors, YAXExceptionTypes.Error);
            var result = ser.Serialize(DirectSelfReferringObject.GetSampleInstanceWithSelfCycle());

            const string expectedResult =
                @"<DirectSelfReferringObject>
  <Data>1</Data>
  <Next />
</DirectSelfReferringObject>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void
            SerializingDirectSelfReferringObjectWithSelfCycleMustThrowWhenThrowUponSerializingCyclingReferencesOptionIsSet()
        {
            Assert.Throws<YAXCannotSerializeSelfReferentialTypes>(() =>
            {
                var ser = new YAXSerializer(typeof(DirectSelfReferringObject),
                    YAXExceptionHandlingPolicies.ThrowWarningsAndErrors, YAXExceptionTypes.Error,
                    YAXSerializationOptions.ThrowUponSerializingCyclingReferences);
                var result = ser.Serialize(DirectSelfReferringObject.GetSampleInstanceWithSelfCycle());
            });
        }

        [Test]
        public void
            InfiniteLoopCausedBySerializingCalculatedPropertiesCanBePreventedBySettingDontSerializePropertiesWithNoSetter()
        {
            var ser = new YAXSerializer(typeof(CalculatedPropertiesCanCauseInfiniteLoop),
                YAXSerializationOptions.DontSerializePropertiesWithNoSetter);
            var result = ser.Serialize(CalculatedPropertiesCanCauseInfiniteLoop.GetSampleInstance());

            const string expectedResult =
                @"<CalculatedPropertiesCanCauseInfiniteLoop>
  <Data>2.0</Data>
</CalculatedPropertiesCanCauseInfiniteLoop>";

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void MaxRecursionPreventsInfiniteLoop()
        {
            var ser = new YAXSerializer(typeof(CalculatedPropertiesCanCauseInfiniteLoop));
            ser.Options.MaxRecursion = 10;
            var result = ser.Serialize(CalculatedPropertiesCanCauseInfiniteLoop.GetSampleInstance());

            const string expectedResult =
                @"<CalculatedPropertiesCanCauseInfiniteLoop>
  <Data>2.0</Data>
  <Reciprocal>
    <Data>0.5</Data>
    <Reciprocal>
      <Data>2</Data>
      <Reciprocal>
        <Data>0.5</Data>
        <Reciprocal>
          <Data>2</Data>
          <Reciprocal>
            <Data>0.5</Data>
            <Reciprocal>
              <Data>2</Data>
              <Reciprocal>
                <Data>0.5</Data>
                <Reciprocal>
                  <Data>2</Data>
                  <Reciprocal />
                </Reciprocal>
              </Reciprocal>
            </Reciprocal>
          </Reciprocal>
        </Reciprocal>
      </Reciprocal>
    </Reciprocal>
  </Reciprocal>
</CalculatedPropertiesCanCauseInfiniteLoop>";

            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(ser.Options.MaxRecursion, Is.EqualTo(10));
            Assert.That(ser.RecursionCount, Is.EqualTo(0));
        }

        [Test]
        public void SerializeExceptionShouldNotThrowExceptions()
        {
            try
            {
                throw new ArgumentOutOfRangeException("index",
                    new InvalidOperationException("Inner exception 1",
                        new Exception("Inner Exception 2")));
            }
            catch (Exception ex)
            {
                var ser = new YAXSerializer(ex.GetType());
                ser.MaxRecursion =
                    10; //todo with the default (300), this takes ages. Even now if 10 this is a really large string
                var exceptionSerialized = ser.Serialize(ex);
                Assert.That(exceptionSerialized, Is.Not.Empty);
            }
        }

        [Test]
        public void PolymorphicDictionaryWithValueAsNull()
        {
            var dict = new Dictionary<string, object>();
            dict.Add("foo", null);
            var serializer = new YAXSerializer(typeof(Dictionary<string, object>));
            var result = serializer.Serialize(dict);

            const string expectedResult =
                @"<DictionaryOfStringObject>
  <KeyValuePairOfStringObject>
    <Key>foo</Key>
    <Value />
  </KeyValuePairOfStringObject>
</DictionaryOfStringObject>";
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void CollectionWithNullElements()
        {
            var list = new List<string>();
            list.Add("1");
            list.Add(null);
            list.Add("3");
            var serializer = new YAXSerializer(typeof(List<string>));
            var result = serializer.Serialize(list);
            const string expectedResult =
                @"<ListOfString>
  <String>1</String>
  <String />
  <String>3</String>
</ListOfString>";
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void PolymorphicCollectionWithNullElements()
        {
            var list = new List<object>();
            list.Add("1");
            list.Add(null);
            list.Add(3);

            var serializer = new YAXSerializer(typeof(List<object>));
            var result = serializer.Serialize(list);
            const string expectedResult =
                @"<ListOfObject xmlns:yaxlib=""http://www.sinairv.com/yaxlib/"">
  <Object yaxlib:realtype=""System.String"">1</Object>
  <Object />
  <Object yaxlib:realtype=""System.Int32"">3</Object>
</ListOfObject>";
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void SerializingNullValues()
        {
            var book = new Book();
            book.Price = 10;
            book.Title = null;

            var ser = new YAXSerializer(typeof(Book));
            var result = ser.Serialize(null);
            const string expectedResult = "<Book />";

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void PolymorphicSerializationOfNullValues()
        {
            var book = new Book();
            book.Price = 10;
            book.Title = null;

            var ser = new YAXSerializer(typeof(object));
            var result = ser.Serialize(null);
            const string expectedResult = "<Object />";

            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }
}