namespace ICSharpCode.SharpZipLib.ArchiveDiag
{
	enum ExtraDataType
	{
		///<summary>Zip64 extended information extra field</summary>
		Zip64 = 0x0001,

		///<summary>AV Info</summary>
		AVInfo = 0x0007,

		///<summary>Reserved for extended language encoding data (PFS) (see APPENDIX D)</summary>
		ExtendedLanguageEncoding = 0x0008,   

		///<summary>OS/2</summary>
		OS2 = 0x0009,

		///<summary>NTFS </summary>
		NTFS = 0x000a,

		///<summary>OpenVMS</summary>
		OpenVMS = 0x000c,

		///<summary>UNIX</summary>
		UNIX = 0x000d,

		///<summary>Reserved for file stream and fork descriptors</summary>
		FileStreamFork = 0x000e,

		///<summary>Patch Descriptor</summary>
		Patch = 0x000f,

		///<summary>PKCS#7 Store for X.509 Certificates</summary>
		PKCS7Certs = 0x0014,

		///<summary>X.509 Certificate ID and Signature for individual file</summary>
		X509FileCert = 0x0015,

		///<summary>X.509 Certificate ID for Central Directory</summary>
		X509CentralDirCert = 0x0016,

		///<summary>Strong Encryption Header</summary>
		Strong = 0x0017,

		///<summary>Record Management Controls</summary>
		Record = 0x0018,

		///<summary>PKCS#7 Encryption Recipient Certificate List</summary>
		PKCS7RecipCerts = 0x0019,

		///<summary>Reserved for Timestamp record</summary>
		Timestamp = 0x0020,

		///<summary>Policy Decryption Key Record</summary>
		PolicyDecryptionKey = 0x0021,

		///<summary>Smartcrypt Key Provider Record</summary>
		SmartcryptKeyProvider = 0x0022,

		///<summary>Smartcrypt Policy Key Data Record</summary>
		SmartcryptPolicyKey = 0x0023,

		///<summary>IBM S/390 (Z390), AS/400 (I400) attributes  - uncompressed</summary>
		IBMUncompressed = 0x0065,

		///<summary>Reserved for IBM S/390 (Z390), AS/400 (I400) attributes - compressed </summary>
		IBMCompressed = 0x0066,

		///<summary>POSZIP 4690 (reserved) </summary>
		POSZIP = 0x4690,

		/// <summary> Info-ZIP Macintosh (old, J. Lee) </summary>
		InfoZipMacOld = 0x07c8,

		/// <summary> ZipIt Macintosh (first version) </summary>
		ZipItMacShort = 0x2605,

		/// <summary> ZipIt Macintosh v 1.3.5 and newer (w/o full filename) </summary>
		ZipItMacLong = 0x2705,

		/// <summary> Info-ZIP Macintosh (new, D. Haase's 'Mac3' field ) </summary>
		InfoZipMacNew = 0x334d,

		/// <summary> Tandem NSK </summary>
		TandemNSK = 0x4154,

		/// <summary> Acorn/SparkFS (David Pilling) </summary>
		AcornSparkFS = 0x4341,

		/// <summary> Windows NT security descriptor (binary ACL) </summary>
		WindowsACL = 0x4453,

		/// <summary> VM/CMS </summary>
		VMCMS = 0x4704,

		/// <summary> MVS </summary>
		MVS = 0x470f,

		/// <summary> Theos, old inofficial port </summary>
		TheosOld = 0x4854,

		/// <summary> FWKCS MD5 (see below) </summary>
		FWKCSMD5 = 0x4b46,

		/// <summary> OS/2 access control list (text ACL) </summary>
		OS2ACL = 0x4c41,

		/// <summary> Info-ZIP OpenVMS (obsolete) </summary>
		InfoZipOpenVMS = 0x4d49,

		/// <summary> Macintosh SmartZIP, by Macro Bambini </summary>
		SmartZipMac = 0x4d63,

		/// <summary> Xceed original location extra field </summary>
		XceedOriginalLocation = 0x4f4c,

		/// <summary> AOS/VS (binary ACL) </summary>
		AOSVSACL = 0x5356,

		/// <summary> extended timestamp </summary>
		UnixExtendedTime = 0x5455,

		/// <summary> Info-ZIP Unix (original; also OS/2, NT, etc.) </summary>
		InfoZipUNIXOld = 0x5855,

		/// <summary> Xceed unicode extra field </summary>
		XceedUnicode = 0x554e,

		/// <summary> BeOS (BeBox, PowerMac, etc.) </summary>
		BeOS = 0x6542,

		/// <summary> Theos </summary>
		Theos = 0x6854,

		/// <summary> ASi Unix </summary>
		ASiUnix = 0x756e,

		/// <summary> Info-ZIP Unix (new) </summary>
		InfoZipUNIXNew = 0x7855,

		/// <summary> SMS/QDOS </summary>
		SMSQDOS = 0xfb4a,

		WinZipAES = 0x9901,

		UnicodeName = 0x7075,
		UnicodeComment = 0x6375,
		CustomCP = 0x5A4C,

		Unknown = 0xffff,
	}
}
