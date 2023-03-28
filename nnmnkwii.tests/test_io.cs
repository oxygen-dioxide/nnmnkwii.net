using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

using nnmnkwii.frontend;
using nnmnkwii.io.hts;
using nnmnkwii.util;

//reference: https://github.com/r9y9/nnmnkwii/blob/master/tests/test_io.py


namespace nnmnkwii.tests
{
    public class test_io
    {
        string DATA_DIR;
        string example_question_file;

        [SetUp]
        public void Setup()
        {
            DATA_DIR = Path.Combine(TestContext.CurrentContext.TestDirectory, "data");
            example_question_file = Path.Combine(DATA_DIR, "example_data/questions-radio_dnn_416.hed");
        }

        [Test]
        public void test_labels_number_of_frames()
        {
            var question_set = hts.load_question_set(Path.Join(DATA_DIR,"jp.hed"));
            var binary_dict = question_set.Item1;
            var numeric_dict = question_set.Item2;
            var labels = hts.load(Path.Join(DATA_DIR, "BASIC5000_0619.lab"));
            var linguistic_features = merlin.linguistic_features(
                labels, binary_dict, numeric_dict,
                add_frame_features:true);
            Assert.AreEqual(labels.num_frames(), linguistic_features.shape[0]);               
        }

        [Test]
        public void test_load_question_set()
        {
            var question_set = hts.load_question_set(example_question_file);
            var binary_dict = question_set.Item1;
            var numeric_dict = question_set.Item2;
            Assert.AreEqual(binary_dict.Count + numeric_dict.Count, 416);
        }

        [Test]
        public void test_htk_style_question_basics(){
            var question_set = hts.load_question_set(Path.Join(DATA_DIR, "test_question.hed"));
            var binary_dict = question_set.Item1;
            // sil k o n i ch i w a sil
            var input_phone_label = Path.Join(DATA_DIR, "hts-nit-atr503", "phrase01.lab");
            var labels = hts.load(input_phone_label);

            //Test if we can handle wildcards correctly
            //also test basic phon contexts (LL, L, C, R, RR)
            var LL_muon1 = binary_dict[0].Item2[0];
            var LL_muon2 = binary_dict[1].Item2[0];
            var L_muon1 = binary_dict[2].Item2[0];
            var C_sil = binary_dict[3].Item2[0];
            var R_phone_o = binary_dict[4].Item2[0];
            var RR_phone_o = binary_dict[5].Item2[0];

            // xx^xx-sil+k=o
            var label = labels[0].context;
            Assert.False(LL_muon1.Match(label).Success);
            Assert.False(LL_muon2.Match(label).Success);
            Assert.False(L_muon1.Match(label).Success);
            Assert.True(C_sil.Match(label).Success);
            Assert.False(R_phone_o.Match(label).Success);
            Assert.True(RR_phone_o.Match(label).Success);

            // xx^sil-k+o=N
            label = labels[1].context;
            Assert.False(LL_muon1.Match(label).Success);
            Assert.False(LL_muon2.Match(label).Success);
            Assert.True(L_muon1.Match(label).Success);
            Assert.False(C_sil.Match(label).Success);
            Assert.True(R_phone_o.Match(label).Success);
            Assert.False(RR_phone_o.Match(label).Success);

            // sil^k-o+N=n
            label = labels[2].context;
            Assert.True(LL_muon1.Match(label).Success);
            Assert.True(LL_muon2.Match(label).Success);
            Assert.False(L_muon1.Match(label).Success);
            Assert.False(C_sil.Match(label).Success);
            Assert.False(R_phone_o.Match(label).Success);
            Assert.False(RR_phone_o.Match(label).Success);
        }

        [Test]
        public void test_singing_voice_question()
        {
            var question_set = hts.load_question_set(
                Path.Join(DATA_DIR, "test_jp_svs.hed"), 
                append_hat_for_LL: false, 
                convert_svs_pattern: true);
            var binary_dict = question_set.Item1;
            var numeric_dict = question_set.Item2;
            var input_phone_label = Path.Join(DATA_DIR, "song070_f00001_063.lab");
            var labels = hts.load(input_phone_label);
            var feats = merlin.linguistic_features(labels, binary_dict, numeric_dict);
            Assert.AreEqual(feats.shape[0], 74);
            Assert.AreEqual(feats.shape[1], 3);

            //CQS e1: get the current MIDI number
            var C_e1 = numeric_dict[0].Item2;
            foreach(var index in Enumerable.Range(0,labels.Count)){
                var lab = labels[index];
                var context = lab.context;
                var match = C_e1.Match(context);
                if(match.Success){
                    Assert.AreEqual(musicmath.NameToTone(match.Value[3..^1]), (float)feats[index, 1]);
                }
            }
            //# CQS e57: get pitch diff
            //# In contrast to other continous features, the pitch diff has a prefix "m" or "p"
            //# to indiecate th sign of numbers.

            var C_e57 = numeric_dict[1].Item2;
            foreach(var index in Enumerable.Range(0, labels.Count))
            {
                var lab = labels[index];
                var context = lab.context;
                var match = C_e57.Match(context);
                if(context.Contains("~p2+")){
                    Assert.AreEqual("p2", match.Groups[1].Value);
                    Assert.AreEqual(2, (float)feats[index, 2]);
                }
                if (context.Contains("~m2+"))
                {
                    Assert.AreEqual("m2", match.Groups[1].Value);
                    Assert.AreEqual(-2, (float)feats[index, 2]);
                }
            }
        }

        [Test]
        public void test_state_alignment_label_file()
        {
            var input_state_label = Path.Join(DATA_DIR, "label_state_align", "arctic_a0001.lab");
            var labels = hts.load(input_state_label);
            var line = File.ReadAllText(input_state_label);
            line = line[^1] == '\n' ? line[..^1] : line;
            Assert.AreEqual(line, labels.ToString());

            Console.WriteLine(labels.num_states());
            Assert.AreEqual(5, labels.num_states());

            //Get and restore durations
            //var durations = merlin.duration_features(labels);
            //TODO
        }
    }
}
