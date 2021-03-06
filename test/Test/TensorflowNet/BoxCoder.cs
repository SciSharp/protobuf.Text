// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: object_detection/protos/box_coder.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Tensorflow.Models.ObjectDetection.Protos {

  /// <summary>Holder for reflection information generated from object_detection/protos/box_coder.proto</summary>
  public static partial class BoxCoderReflection {

    #region Descriptor
    /// <summary>File descriptor for object_detection/protos/box_coder.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static BoxCoderReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CidvYmplY3RfZGV0ZWN0aW9uL3Byb3Rvcy9ib3hfY29kZXIucHJvdG8SF29i",
            "amVjdF9kZXRlY3Rpb24ucHJvdG9zGjNvYmplY3RfZGV0ZWN0aW9uL3Byb3Rv",
            "cy9mYXN0ZXJfcmNubl9ib3hfY29kZXIucHJvdG8aMG9iamVjdF9kZXRlY3Rp",
            "b24vcHJvdG9zL2tleXBvaW50X2JveF9jb2Rlci5wcm90bxozb2JqZWN0X2Rl",
            "dGVjdGlvbi9wcm90b3MvbWVhbl9zdGRkZXZfYm94X2NvZGVyLnByb3RvGi5v",
            "YmplY3RfZGV0ZWN0aW9uL3Byb3Rvcy9zcXVhcmVfYm94X2NvZGVyLnByb3Rv",
            "IscCCghCb3hDb2RlchJMChVmYXN0ZXJfcmNubl9ib3hfY29kZXIYASABKAsy",
            "Ky5vYmplY3RfZGV0ZWN0aW9uLnByb3Rvcy5GYXN0ZXJSY25uQm94Q29kZXJI",
            "ABJMChVtZWFuX3N0ZGRldl9ib3hfY29kZXIYAiABKAsyKy5vYmplY3RfZGV0",
            "ZWN0aW9uLnByb3Rvcy5NZWFuU3RkZGV2Qm94Q29kZXJIABJDChBzcXVhcmVf",
            "Ym94X2NvZGVyGAMgASgLMicub2JqZWN0X2RldGVjdGlvbi5wcm90b3MuU3F1",
            "YXJlQm94Q29kZXJIABJHChJrZXlwb2ludF9ib3hfY29kZXIYBCABKAsyKS5v",
            "YmplY3RfZGV0ZWN0aW9uLnByb3Rvcy5LZXlwb2ludEJveENvZGVySABCEQoP",
            "Ym94X2NvZGVyX29uZW9mYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Tensorflow.Models.ObjectDetection.Protos.FasterRcnnBoxCoderReflection.Descriptor, global::Tensorflow.Models.ObjectDetection.Protos.KeypointBoxCoderReflection.Descriptor, global::Tensorflow.Models.ObjectDetection.Protos.MeanStddevBoxCoderReflection.Descriptor, global::Tensorflow.Models.ObjectDetection.Protos.SquareBoxCoderReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Tensorflow.Models.ObjectDetection.Protos.BoxCoder), global::Tensorflow.Models.ObjectDetection.Protos.BoxCoder.Parser, new[]{ "FasterRcnnBoxCoder", "MeanStddevBoxCoder", "SquareBoxCoder", "KeypointBoxCoder" }, new[]{ "BoxCoderOneof" }, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// Configuration proto for the box coder to be used in the object detection
  /// pipeline. See core/box_coder.py for details.
  /// </summary>
  public sealed partial class BoxCoder : pb::IMessage<BoxCoder> {
    private static readonly pb::MessageParser<BoxCoder> _parser = new pb::MessageParser<BoxCoder>(() => new BoxCoder());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<BoxCoder> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Tensorflow.Models.ObjectDetection.Protos.BoxCoderReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BoxCoder() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BoxCoder(BoxCoder other) : this() {
      switch (other.BoxCoderOneofCase) {
        case BoxCoderOneofOneofCase.FasterRcnnBoxCoder:
          FasterRcnnBoxCoder = other.FasterRcnnBoxCoder.Clone();
          break;
        case BoxCoderOneofOneofCase.MeanStddevBoxCoder:
          MeanStddevBoxCoder = other.MeanStddevBoxCoder.Clone();
          break;
        case BoxCoderOneofOneofCase.SquareBoxCoder:
          SquareBoxCoder = other.SquareBoxCoder.Clone();
          break;
        case BoxCoderOneofOneofCase.KeypointBoxCoder:
          KeypointBoxCoder = other.KeypointBoxCoder.Clone();
          break;
      }

      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BoxCoder Clone() {
      return new BoxCoder(this);
    }

    /// <summary>Field number for the "faster_rcnn_box_coder" field.</summary>
    public const int FasterRcnnBoxCoderFieldNumber = 1;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Tensorflow.Models.ObjectDetection.Protos.FasterRcnnBoxCoder FasterRcnnBoxCoder {
      get { return boxCoderOneofCase_ == BoxCoderOneofOneofCase.FasterRcnnBoxCoder ? (global::Tensorflow.Models.ObjectDetection.Protos.FasterRcnnBoxCoder) boxCoderOneof_ : null; }
      set {
        boxCoderOneof_ = value;
        boxCoderOneofCase_ = value == null ? BoxCoderOneofOneofCase.None : BoxCoderOneofOneofCase.FasterRcnnBoxCoder;
      }
    }

    /// <summary>Field number for the "mean_stddev_box_coder" field.</summary>
    public const int MeanStddevBoxCoderFieldNumber = 2;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Tensorflow.Models.ObjectDetection.Protos.MeanStddevBoxCoder MeanStddevBoxCoder {
      get { return boxCoderOneofCase_ == BoxCoderOneofOneofCase.MeanStddevBoxCoder ? (global::Tensorflow.Models.ObjectDetection.Protos.MeanStddevBoxCoder) boxCoderOneof_ : null; }
      set {
        boxCoderOneof_ = value;
        boxCoderOneofCase_ = value == null ? BoxCoderOneofOneofCase.None : BoxCoderOneofOneofCase.MeanStddevBoxCoder;
      }
    }

    /// <summary>Field number for the "square_box_coder" field.</summary>
    public const int SquareBoxCoderFieldNumber = 3;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Tensorflow.Models.ObjectDetection.Protos.SquareBoxCoder SquareBoxCoder {
      get { return boxCoderOneofCase_ == BoxCoderOneofOneofCase.SquareBoxCoder ? (global::Tensorflow.Models.ObjectDetection.Protos.SquareBoxCoder) boxCoderOneof_ : null; }
      set {
        boxCoderOneof_ = value;
        boxCoderOneofCase_ = value == null ? BoxCoderOneofOneofCase.None : BoxCoderOneofOneofCase.SquareBoxCoder;
      }
    }

    /// <summary>Field number for the "keypoint_box_coder" field.</summary>
    public const int KeypointBoxCoderFieldNumber = 4;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Tensorflow.Models.ObjectDetection.Protos.KeypointBoxCoder KeypointBoxCoder {
      get { return boxCoderOneofCase_ == BoxCoderOneofOneofCase.KeypointBoxCoder ? (global::Tensorflow.Models.ObjectDetection.Protos.KeypointBoxCoder) boxCoderOneof_ : null; }
      set {
        boxCoderOneof_ = value;
        boxCoderOneofCase_ = value == null ? BoxCoderOneofOneofCase.None : BoxCoderOneofOneofCase.KeypointBoxCoder;
      }
    }

    private object boxCoderOneof_;
    /// <summary>Enum of possible cases for the "box_coder_oneof" oneof.</summary>
    public enum BoxCoderOneofOneofCase {
      None = 0,
      FasterRcnnBoxCoder = 1,
      MeanStddevBoxCoder = 2,
      SquareBoxCoder = 3,
      KeypointBoxCoder = 4,
    }
    private BoxCoderOneofOneofCase boxCoderOneofCase_ = BoxCoderOneofOneofCase.None;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BoxCoderOneofOneofCase BoxCoderOneofCase {
      get { return boxCoderOneofCase_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void ClearBoxCoderOneof() {
      boxCoderOneofCase_ = BoxCoderOneofOneofCase.None;
      boxCoderOneof_ = null;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as BoxCoder);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(BoxCoder other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(FasterRcnnBoxCoder, other.FasterRcnnBoxCoder)) return false;
      if (!object.Equals(MeanStddevBoxCoder, other.MeanStddevBoxCoder)) return false;
      if (!object.Equals(SquareBoxCoder, other.SquareBoxCoder)) return false;
      if (!object.Equals(KeypointBoxCoder, other.KeypointBoxCoder)) return false;
      if (BoxCoderOneofCase != other.BoxCoderOneofCase) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.FasterRcnnBoxCoder) hash ^= FasterRcnnBoxCoder.GetHashCode();
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.MeanStddevBoxCoder) hash ^= MeanStddevBoxCoder.GetHashCode();
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.SquareBoxCoder) hash ^= SquareBoxCoder.GetHashCode();
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.KeypointBoxCoder) hash ^= KeypointBoxCoder.GetHashCode();
      hash ^= (int) boxCoderOneofCase_;
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.FasterRcnnBoxCoder) {
        output.WriteRawTag(10);
        output.WriteMessage(FasterRcnnBoxCoder);
      }
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.MeanStddevBoxCoder) {
        output.WriteRawTag(18);
        output.WriteMessage(MeanStddevBoxCoder);
      }
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.SquareBoxCoder) {
        output.WriteRawTag(26);
        output.WriteMessage(SquareBoxCoder);
      }
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.KeypointBoxCoder) {
        output.WriteRawTag(34);
        output.WriteMessage(KeypointBoxCoder);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.FasterRcnnBoxCoder) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(FasterRcnnBoxCoder);
      }
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.MeanStddevBoxCoder) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(MeanStddevBoxCoder);
      }
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.SquareBoxCoder) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(SquareBoxCoder);
      }
      if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.KeypointBoxCoder) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(KeypointBoxCoder);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(BoxCoder other) {
      if (other == null) {
        return;
      }
      switch (other.BoxCoderOneofCase) {
        case BoxCoderOneofOneofCase.FasterRcnnBoxCoder:
          if (FasterRcnnBoxCoder == null) {
            FasterRcnnBoxCoder = new global::Tensorflow.Models.ObjectDetection.Protos.FasterRcnnBoxCoder();
          }
          FasterRcnnBoxCoder.MergeFrom(other.FasterRcnnBoxCoder);
          break;
        case BoxCoderOneofOneofCase.MeanStddevBoxCoder:
          if (MeanStddevBoxCoder == null) {
            MeanStddevBoxCoder = new global::Tensorflow.Models.ObjectDetection.Protos.MeanStddevBoxCoder();
          }
          MeanStddevBoxCoder.MergeFrom(other.MeanStddevBoxCoder);
          break;
        case BoxCoderOneofOneofCase.SquareBoxCoder:
          if (SquareBoxCoder == null) {
            SquareBoxCoder = new global::Tensorflow.Models.ObjectDetection.Protos.SquareBoxCoder();
          }
          SquareBoxCoder.MergeFrom(other.SquareBoxCoder);
          break;
        case BoxCoderOneofOneofCase.KeypointBoxCoder:
          if (KeypointBoxCoder == null) {
            KeypointBoxCoder = new global::Tensorflow.Models.ObjectDetection.Protos.KeypointBoxCoder();
          }
          KeypointBoxCoder.MergeFrom(other.KeypointBoxCoder);
          break;
      }

      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            global::Tensorflow.Models.ObjectDetection.Protos.FasterRcnnBoxCoder subBuilder = new global::Tensorflow.Models.ObjectDetection.Protos.FasterRcnnBoxCoder();
            if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.FasterRcnnBoxCoder) {
              subBuilder.MergeFrom(FasterRcnnBoxCoder);
            }
            input.ReadMessage(subBuilder);
            FasterRcnnBoxCoder = subBuilder;
            break;
          }
          case 18: {
            global::Tensorflow.Models.ObjectDetection.Protos.MeanStddevBoxCoder subBuilder = new global::Tensorflow.Models.ObjectDetection.Protos.MeanStddevBoxCoder();
            if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.MeanStddevBoxCoder) {
              subBuilder.MergeFrom(MeanStddevBoxCoder);
            }
            input.ReadMessage(subBuilder);
            MeanStddevBoxCoder = subBuilder;
            break;
          }
          case 26: {
            global::Tensorflow.Models.ObjectDetection.Protos.SquareBoxCoder subBuilder = new global::Tensorflow.Models.ObjectDetection.Protos.SquareBoxCoder();
            if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.SquareBoxCoder) {
              subBuilder.MergeFrom(SquareBoxCoder);
            }
            input.ReadMessage(subBuilder);
            SquareBoxCoder = subBuilder;
            break;
          }
          case 34: {
            global::Tensorflow.Models.ObjectDetection.Protos.KeypointBoxCoder subBuilder = new global::Tensorflow.Models.ObjectDetection.Protos.KeypointBoxCoder();
            if (boxCoderOneofCase_ == BoxCoderOneofOneofCase.KeypointBoxCoder) {
              subBuilder.MergeFrom(KeypointBoxCoder);
            }
            input.ReadMessage(subBuilder);
            KeypointBoxCoder = subBuilder;
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
