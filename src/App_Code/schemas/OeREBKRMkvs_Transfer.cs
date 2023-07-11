/* $Rev: 29789 $ */
namespace OeREBKRMkvs_V2_0
{
    using System;
    using System.Diagnostics;
    using System.Xml.Serialization;
    using System.Runtime.Serialization;
    using System.Collections;
    using System.Xml.Schema;
    using System.ComponentModel;
    using System.Xml;

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3761.0")]
    [Serializable]
    [DebuggerStepThrough]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.interlis.ch/INTERLIS2.3")]
    [XmlRootAttribute(Namespace = "http://www.interlis.ch/INTERLIS2.3", IsNullable = false)]
    public partial class TRANSFER
    {
        #region Private fields
        private TRANSFERHEADERSECTION _hEADERSECTION;
        private TRANSFERDATASECTION _dATASECTION;
        #endregion

        public TRANSFER()
        {
            _dATASECTION = new TRANSFERDATASECTION();
            _hEADERSECTION = new TRANSFERHEADERSECTION();
        }

        public TRANSFERHEADERSECTION HEADERSECTION
        {
            get
            {
                return _hEADERSECTION;
            }
            set
            {
                _hEADERSECTION = value;
            }
        }

        public TRANSFERDATASECTION DATASECTION
        {
            get
            {
                return _dATASECTION;
            }
            set
            {
                _dATASECTION = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3761.0")]
    [Serializable]
    [DebuggerStepThrough]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.interlis.ch/INTERLIS2.3")]
    public partial class TRANSFERHEADERSECTION
    {
        #region Private fields
        private TRANSFERHEADERSECTIONMODEL[] _mODELS;
        private string _cOMMENT;
        private string _sENDER;
        private decimal _vERSION;
        #endregion

        [XmlArrayItemAttribute("MODEL", IsNullable = false)]
        public TRANSFERHEADERSECTIONMODEL[] MODELS
        {
            get
            {
                return _mODELS;
            }
            set
            {
                _mODELS = value;
            }
        }

        public string COMMENT
        {
            get
            {
                return _cOMMENT;
            }
            set
            {
                _cOMMENT = value;
            }
        }

        [XmlAttribute]
        public string SENDER
        {
            get
            {
                return _sENDER;
            }
            set
            {
                _sENDER = value;
            }
        }

        [XmlAttribute]
        public decimal VERSION
        {
            get
            {
                return _vERSION;
            }
            set
            {
                _vERSION = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3761.0")]
    [Serializable]
    [DebuggerStepThrough]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.interlis.ch/INTERLIS2.3")]
    public partial class TRANSFERHEADERSECTIONMODEL
    {
        #region Private fields
        private string _nAME;
        private System.DateTime _vERSION;
        private string _uRI;
        #endregion

        [XmlAttribute]
        public string NAME
        {
            get
            {
                return _nAME;
            }
            set
            {
                _nAME = value;
            }
        }

        [XmlAttribute(DataType = "date")]
        public System.DateTime VERSION
        {
            get
            {
                return _vERSION;
            }
            set
            {
                _vERSION = value;
            }
        }

        [XmlAttribute]
        public string URI
        {
            get
            {
                return _uRI;
            }
            set
            {
                _uRI = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3761.0")]
    [Serializable]
    [DebuggerStepThrough]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.interlis.ch/INTERLIS2.3")]
    public partial class TRANSFERDATASECTION
    {
        #region Private fields
        private TRANSFERDATASECTIONOeREBKRM_V2_0Dokumente _oeREBKRM_V2_0Dokumente;
        private TRANSFERDATASECTIONOeREBKRMkvs_V2_0Konfiguration _oeREBKRMkvs_V2_0Konfiguration;
        private TRANSFERDATASECTIONOeREBKRMkvs_V2_0Thema _oeREBKRMkvs_V2_0Thema;
        #endregion

        public TRANSFERDATASECTION()
        {
            _oeREBKRM_V2_0Dokumente = new TRANSFERDATASECTIONOeREBKRM_V2_0Dokumente();
            _oeREBKRMkvs_V2_0Konfiguration = new TRANSFERDATASECTIONOeREBKRMkvs_V2_0Konfiguration();
            _oeREBKRMkvs_V2_0Thema = new TRANSFERDATASECTIONOeREBKRMkvs_V2_0Thema();            
        }

        [XmlElement("OeREBKRM_V2_0.Dokumente")]
        public TRANSFERDATASECTIONOeREBKRM_V2_0Dokumente OeREBKRM_V2_0Dokumente
        {
            get
            {
                return _oeREBKRM_V2_0Dokumente;
            }
            set
            {
                _oeREBKRM_V2_0Dokumente = value;
            }
        }

        [XmlElement("OeREBKRMkvs_V2_0.Konfiguration")]
        public TRANSFERDATASECTIONOeREBKRMkvs_V2_0Konfiguration OeREBKRMkvs_V2_0Konfiguration
        {
            get
            {
                return _oeREBKRMkvs_V2_0Konfiguration;
            }
            set
            {
                _oeREBKRMkvs_V2_0Konfiguration = value;
            }
        }

        [XmlElement("OeREBKRMkvs_V2_0.Thema")]
        public TRANSFERDATASECTIONOeREBKRMkvs_V2_0Thema OeREBKRMkvs_V2_0Thema
        {
            get
            {
                return _oeREBKRMkvs_V2_0Thema;
            }
            set
            {
                _oeREBKRMkvs_V2_0Thema = value;
            }
        }
    }
}