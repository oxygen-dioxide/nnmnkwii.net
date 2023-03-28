using NumSharp.Generic;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using nnmnkwii.io.hts;
//reference: https://github.com/r9y9/nnmnkwii/blob/master/nnmnkwii/frontend/merlin.py

namespace nnmnkwii.frontend {
    public enum FeatureType {binary, numerical}

    public enum UnitSize {phoneme, state}

    public enum FeatureSize {phoneme, frame}

    public class merlin {
        //TODO:Should subphone_features be an enum?
        static Dictionary<string, int> frame_feature_size_dict = new Dictionary<string, int>
        {
            {"full",9},
            {"state_only",1 },
            {"frame_only",1 },
            {"uniform_state",2 },
            {"minimal_phoneme",3 },
            {"coarse_coding",4 },
        };

        public static int get_frame_feature_size(string subphone_features = "full") {
            if (subphone_features == null) {
                return 0;
            }
            subphone_features = subphone_features.Trim().ToLower();
            if (subphone_features == "none") {
                //TODO:raise ValueError("subphone_features = 'none' is deprecated, use None instead")
                throw new Exception("subphone_features = 'none' is deprecated, use None instead");
            }
            if (frame_feature_size_dict.TryGetValue(subphone_features, out var result)) {
                return result;
            } else {
                //TODO:raise ValueError("Unknown value for subphone_features: %s" % (subphone_features))
                throw new Exception($"Unknown value for subphone_features: {subphone_features}");
            }
        }

        public static NDArray<float> pattern_matching_binary(
            Dictionary<int, Tuple<string, List<Regex>>> binary_dict, string label) {
            int dict_size = binary_dict.Count;
            var lab_binary_vector = np.zeros<float>(new int[]{ dict_size }).AsGeneric<float>();
            foreach (int i in Enumerable.Range(0, dict_size)) {
                //ignored code: Always true
                //if isinstance(current_question_list, tuple):
                var current_question_list = binary_dict[i].Item2;
                var binary_flag = current_question_list
                    .Any(current_compiled => current_compiled.Match(label).Success) ? 1 : 0;
                lab_binary_vector[i] = binary_flag;
            }
            return lab_binary_vector;
        }

        public static NDArray<float> pattern_matching_continous_position(
            Dictionary<int, Tuple<string, Regex>> numeric_dict, string label) {
            int dict_size = numeric_dict.Count;
            var lab_continuous_vector = np.zeros<float>(new int[] { dict_size }).AsGeneric<float>();
            foreach (int i in Enumerable.Range(0, dict_size)) {
                //ignored code: Always true
                //if isinstance(current_compiled, tuple):

                var current_compiled = numeric_dict[i].Item2;
                //# NOTE: newer version returns tuple of (name, question)

                //ignore code:
                //if isinstance(current_compiled, tuple):
                //  current_compiled = current_compiled[1]
                float continuous_value;
                if (current_compiled.ToString().Contains("([-\\d]+)")) {
                    continuous_value = -50.0f;
                } else {
                    continuous_value = -1.0f;
                }

                var ms = current_compiled.Match(label);
                if (ms.Success) {
                    string note = ms.Groups[1].Value;
                    var tone = util.musicmath.NameToTone(note);
                    if (tone>0) {
                        continuous_value = tone;
                    } else if (note.StartsWith("p")) {
                        continuous_value = int.Parse(note[1..]);
                    } else if (note.StartsWith("m")) {
                        continuous_value = -int.Parse(note[1..]);
                    } else if (float.TryParse(note, out float num)) {
                        continuous_value = num;
                    }
                    
                }
                lab_continuous_vector[i] = continuous_value;
            }
            return lab_continuous_vector;
        }

        public static NDArray load_labels_with_phone_alignment(
            HTSLabelFile hts_labels,
            Dictionary<int, Tuple<string, List<Regex>>> binary_dict,
            Dictionary<int, Tuple<string, Regex>> numeric_dict,
            string subphone_features = null,
            bool add_frame_features = false,
            int frame_shift = 50000
        ) {
            int dict_size = binary_dict.Count + numeric_dict.Count;
            int frame_feature_size = get_frame_feature_size(subphone_features);
            int featuresDim = frame_feature_size + dict_size;

            //number of frames or phonemes, determined by add_frame_features
            int tCount;
            if (add_frame_features) {
                tCount = hts_labels.num_frames();
            } else {
                tCount = hts_labels.num_phones();
            }
            int label_feature_index = 0;

            //matrix size: tCount*dimension
            var label_feature_matrix = np.zeros<float>(tCount, featuresDim);
            if (subphone_features == "coarse_coding") {
                throw new NotImplementedException();
                //TODO:compute_coarse_coding_features()
            }
            foreach (int phonemeId in Enumerable.Range(0, hts_labels.Count)) {
                var label = hts_labels[phonemeId];
                var frame_number = label.end_time / frame_shift - label.start_time / frame_shift;
                //label_binary_vector = pattern_matching_binary(binary_dict, full_label)
                var label_vector = pattern_matching_binary(binary_dict, label.context).astype(np.float32);

                var label_continuous_vector = pattern_matching_continous_position(numeric_dict, label.context);
                label_vector = np.concatenate(new NDArray[] { label_vector, label_continuous_vector });
                //label_vector.AddRange(label_continuous_vector);

                /*TODO:
                 if subphone_features == "coarse_coding":
                    cc_feat_matrix = extract_coarse_coding_features_relative(
                        cc_features, frame_number)
                 */
                if (add_frame_features) {
                    var current_block_binary_array = np.ones<float>(
                        new int[]{ frame_number, 1 }
                        ).dot(label_vector[Slice.NewAxis, Slice.All]);
                    if (subphone_features == "minimal_phoneme")
                    {
                        //features which distinguish frame position in phoneme
                        //fraction through phone forwards
                        var frForward = np.linspace(1 / frame_number, 1, frame_number);
                        current_block_binary_array[Slice.All, dict_size] = frForward;
                        //fraction through phone backwards
                        current_block_binary_array[Slice.All, dict_size + 1] = 1 + 1 / frame_number - frForward;
                        //phone duration
                        current_block_binary_array[Slice.All, dict_size + 2] = frame_number;
                    } else if(subphone_features == "coarse_coding")
                    {
                        /*TODO
                        //features which distinguish frame position in phoneme
                        //using three continous numerical features
                        current_block_binary_array[i, dict_size + 0] = cc_feat_matrix[i, 0];
                        current_block_binary_array[i, dict_size + 1] = cc_feat_matrix[i, 1];
                        current_block_binary_array[i, dict_size + 2] = cc_feat_matrix[i, 2];
                        current_block_binary_array[i, dict_size + 3] = frame_number;*/
                    } else if (String.IsNullOrEmpty(subphone_features))
                    {

                    }
                    else
                    {
                        throw new Exception($"Combination of subphone_features and add_frame_features "
                            + "is not supported: {subphone_features}, {add_frame_features}");
                    }
                    label_feature_matrix[
                        new Slice(label_feature_index, label_feature_index + frame_number), Slice.All
                        ] = current_block_binary_array;
                    label_feature_index = label_feature_index + frame_number;
                } else if (subphone_features == null) {
                    label_feature_matrix[phonemeId] = label_vector;
                }
            }
            //#omg
            //TODO
            /*
             if label_feature_index == 0:
            raise ValueError(
                "Combination of subphone_features and add_frame_features is not supported: {}, {}".format(
                    subphone_features, add_frame_features
                    ))
             */
            return label_feature_matrix;
        }

        public static NDArray linguistic_features(
            HTSLabelFile hts_labels,
            Dictionary<int, Tuple<string, List<Regex>>> binary_dict,
            Dictionary<int, Tuple<string, Regex>> numeric_dict,
            string subphone_features = null,
            bool add_frame_features = false,
            int frame_shift = 50000
            ) {
            if (hts_labels.is_state_alignment_label()) {
                throw new NotImplementedException();
                //TODO:load_labels_with_state_alignment
            } else {
                return load_labels_with_phone_alignment(
                    hts_labels,
                    binary_dict,
                    numeric_dict,
                    subphone_features,
                    add_frame_features,
                    frame_shift
                    );
            }
        }
    
        public NDArray extrace_dur_from_state_alignment_labels(
            HTSLabelFile hts_labels,
            FeatureType feature_type = FeatureType.numerical,
            UnitSize unit_size = UnitSize.state,
            FeatureSize feature_size = FeatureSize.phoneme,
            int frame_shift = 50000
        ) {
            var dur_dim = unit_size == UnitSize.state 
                ? hts_labels.num_states() 
                : 1;
            var tCount = feature_size == FeatureSize.phoneme 
                ? hts_labels.num_phones() 
                : hts_labels.num_frames();
            var dur_feature_matrix = np.zeros<int>(new int[]{tCount, dur_dim});

            var current_dur_array = np.zeros(new int[]{dur_dim, 1});
            var state_number = hts_labels.num_states();
            dur_dim = state_number;

            var dur_feature_index = 0;
            int phone_duration = 0;
            foreach(var current_index in Enumerable.Range(0, hts_labels.Count)) {
                var label = hts_labels[current_index];
                var start_time = label.start_time;
                var end_time = label.end_time;
                var full_label = label.context;
                //remove state information [k]
                var full_label_length = label.context.Length - 3;
                var state_index = full_label[full_label_length];
                var state_index_int = int.Parse(state_index.ToString());

                var frame_number = (end_time - start_time) / frame_shift;
                if(state_index==1){
                    phone_duration = frame_number;
                    foreach(int i in Enumerable.Range(0, state_number - 1)) {
                        var l = hts_labels[current_index + i + 1];
                        phone_duration += (l.end_time - l.start_time) / frame_shift;
                    }
                }

                NDArray current_block_array = null;
                if(feature_type == FeatureType.binary){
                    current_block_array = np.zeros<int>(new int[]{frame_number, 1});
                    if(unit_size == UnitSize.state){
                        current_block_array[^1] = 1;
                    } else if(unit_size == UnitSize.phoneme){
                        if(state_index_int == state_number){
                            current_block_array[^1] = 1;
                        }
                    }
                }else{
                    if(unit_size == UnitSize.state){
                        current_dur_array[current_index % 5] = frame_number;
                        if(feature_size ==FeatureSize.phoneme && state_index_int == state_number){
                            current_block_array = current_dur_array.T;
                        }
                        if(feature_size == FeatureSize.frame){
                            current_block_array = np.ones<float>(new int[]{frame_number, 1}).dot(current_dur_array.T);
                        }
                    } else if(unit_size == UnitSize.phoneme){
                        current_block_array = np.array(phone_duration);
                    }
                }
                if(feature_size==FeatureSize.frame){
                    dur_feature_matrix[
                        new Slice(dur_feature_index, dur_feature_index + frame_number), Slice.All
                        ] = current_block_array;
                    dur_feature_index = dur_feature_index + frame_number;
                } else if(feature_size == FeatureSize.phoneme){
                    dur_feature_matrix[dur_feature_index, Slice.All] = current_block_array;
                    dur_feature_index = dur_feature_index + 1;
                }
            }
            dur_feature_matrix = dur_feature_matrix[
                new Slice(0, dur_feature_index), Slice.All
                ];
            return dur_feature_index;
        }
    
        public NDArray duration_features(
            HTSLabelFile hts_labels,
            FeatureType feature_type = FeatureType.numerical,
            UnitSize unit_size = UnitSize.state,
            FeatureSize feature_size = FeatureSize.phoneme,
            int frame_shift = 50000
        ){
            if(hts_labels.is_state_alignment_label()){
                return extrace_dur_from_state_alignment_labels(
                    hts_labels,
                    feature_type,
                    unit_size,
                    feature_size,
                    frame_shift
                    );
            } else {
                throw new NotImplementedException();
            }
        }
    }
}
