/* $Rev: 19053 $ */
namespace DataExtract.Gml.Simplified
{
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public abstract class AbstractGMLType
    {
        private string _id;

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "ID")]
        public string id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
            }
        }
    }

    public abstract class AbstractGeometryType : AbstractGMLType
    {
        private string _srsName;
        private string _srsDimension;

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
        public string srsName
        {
            get
            {
                return this._srsName;
            }
            set
            {
                this._srsName = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
        public string srsDimension
        {
            get
            {
                return this._srsDimension;
            }
            set
            {
                this._srsDimension = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class LinearRingType
    {
        private string _posList;

        [System.Xml.Serialization.XmlElementAttribute("posList")]
        public string posList
        {
            get
            {
                return this._posList;
            }
            set
            {
                this._posList = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class AbstractRingPropertyType
    {
        private LinearRingType _linearRing;

        public LinearRingType LinearRing
        {
            get
            {
                return this._linearRing;
            }
            set
            {
                this._linearRing = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class PolygonType : AbstractGeometryType
    {
        private AbstractRingPropertyType _exterior;
        private AbstractRingPropertyType[] _interior;

        public AbstractRingPropertyType exterior
        {
            get
            {
                return this._exterior;
            }
            set
            {
                this._exterior = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("interior")]
        public AbstractRingPropertyType[] interior
        {
            get
            {
                return this._interior;
            }
            set
            {
                this._interior = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class SurfacePropertyType
    {
        private PolygonType _polygon;
        private string _nilReason;
        private string _remoteSchema;
        private bool _owns;

        public PolygonType Polygon
        {
            get
            {
                return this._polygon;
            }
            set
            {
                this._polygon = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string nilReason
        {
            get
            {
                return this._nilReason;
            }
            set
            {
                this._nilReason = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
        public string remoteSchema
        {
            get
            {
                return this._remoteSchema;
            }
            set
            {
                this._remoteSchema = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool owns
        {
            get
            {
                return this._owns;
            }
            set
            {
                this._owns = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.opengis.net/gml/3.2")]
    public class MultiSurfaceType : AbstractGeometryType
    {
        private SurfacePropertyType[] _surfaceMember;

        [System.Xml.Serialization.XmlElementAttribute("surfaceMember")]
        public SurfacePropertyType[] surfaceMember
        {
            get
            {
                return this._surfaceMember;
            }
            set
            {
                this._surfaceMember = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class MultiSurfacePropertyType
    {

        private MultiSurfaceType _multiSurface;
        private string _nilReason;
        private string _remoteSchema;
        private bool _owns;

        public MultiSurfaceType MultiSurface
        {
            get
            {
                return this._multiSurface;
            }
            set
            {
                this._multiSurface = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string nilReason
        {
            get
            {
                return this._nilReason;
            }
            set
            {
                this._nilReason = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
        public string remoteSchema
        {
            get
            {
                return this._remoteSchema;
            }
            set
            {
                this._remoteSchema = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool owns
        {
            get
            {
                return this._owns;
            }
            set
            {
                this._owns = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class LinearStringType : AbstractGeometryType
    {
        private string _posList;

        [System.Xml.Serialization.XmlElementAttribute("posList")]
        public string posList
        {
            get
            {
                return this._posList;
            }
            set
            {
                this._posList = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class PointType : AbstractGeometryType
    {
        private string _pos;

        [System.Xml.Serialization.XmlElementAttribute("pos")]
        public string pos
        {
            get
            {
                return this._pos;
            }
            set
            {
                this._pos = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class AbstractGeometricPrimitiveType
    {
        private PointType _point;

        public PointType Point
        {
            get
            {
                return this._point;
            }
            set
            {
                this._point = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class CurvePropertyType
    {
        private LinearStringType _lineString;
        private string _nilReason;
        private string _remoteSchema;
        private bool _owns;

        public LinearStringType LineString
        {
            get
            {
                return this._lineString;
            }
            set
            {
                this._lineString = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string nilReason
        {
            get
            {
                return this._nilReason;
            }
            set
            {
                this._nilReason = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
        public string remoteSchema
        {
            get
            {
                return this._remoteSchema;
            }
            set
            {
                this._remoteSchema = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool owns
        {
            get
            {
                return this._owns;
            }
            set
            {
                this._owns = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml/3.2")]
    public class PointPropertyType
    {
        private PointType _point;
        private string _nilReason;
        private string _remoteSchema;
        private bool _owns;
        
        public PointType Point
        {
            get
            {
                return this._point;
            }
            set
            {
                this._point = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string nilReason
        {
            get
            {
                return this._nilReason;
            }
            set
            {
                this._nilReason = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
        public string remoteSchema
        {
            get
            {
                return this._remoteSchema;
            }
            set
            {
                this._remoteSchema = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool owns
        {
            get
            {
                return this._owns;
            }
            set
            {
                this._owns = value;
            }
        }
    }
}