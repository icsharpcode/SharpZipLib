namespace ICSharpCode.SharpZipLib.ArchiveDiag
{
	struct ZipVersion
	{
		public enum ZipVersionOS: byte
		{
			MsDOS = 0,
			Amiga = 1,
			OpenVMS = 2,
			UNIX = 3,
			VMCMS = 4,
			AtariST = 5,
			OS2 = 6,
			Macintosh = 7,
			ZSystem = 8,
			CPM = 9,
			Windows = 10,
			MVS = 11,
			VSE = 12,
			AcornRisc = 13,
			VFAT = 14,
			AlternateMVS = 15,
			BeOS = 16,
			Tandem = 17,
			OS400 = 18,
			MacOS = 19
		}

		public ZipVersionOS OperatingSystem;
		public byte Major;
		public byte Minor;

		public byte OSRaw => (byte) OperatingSystem;

		public ZipVersion(byte major, byte minor, ZipVersionOS os)
		{
			Major = major;
			Minor = minor;
			OperatingSystem = os;
		}

		public static ZipVersion From(ushort versionBytes)
			=> new ZipVersion(
				(byte)((versionBytes & 0x00ff) / 10),
				(byte)((versionBytes & 0x00ff) % 10),
				(ZipVersionOS) (versionBytes >> 8));

		public override string ToString()
			=> $"v{Major}.{Minor} / {OperatingSystem:G}";
	}
}
