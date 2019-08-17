namespace BuildingCrafter
{
	[System.Flags]
	public enum WindowTypeEnum
	{
		Standard = 0,
		Short = 2,
		Medium = 4,
		Tall2p8 = 8,
		Tall2p5 = 16,
		HighSmall = 32,
		Override = 32768,
	}

	// NOTE: Adding anything here must be added to the Building Style Panel on the enum flag setter
}