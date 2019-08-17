namespace BuildingCrafter
{
	[System.Flags]
	public enum DoorTypeEnum
	{
		Standard = 0,
		Heavy = 2,
		Open = 4,
		SkinnyOpen = 8,
		Closet = 16,
		TallOpen = 32,
		DoorToRoof = 64,
		//Override = 32768,
	}
}