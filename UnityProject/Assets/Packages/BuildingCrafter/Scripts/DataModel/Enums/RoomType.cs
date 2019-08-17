namespace BuildingCrafter
{
	[System.Flags]
	public enum RoomType
	{
		Generic = 0,
		LivingRoom = 1,
		Bedroom = 2,
		Closet = 4,
		Hallways = 8,
		Kitchen = 16,
		Dining = 32,
		Bathroom = 64,
		Kids = 128,
		Utility = 256,
		Patio = 512,
		Garage = 1024,
		Office = 2048,
		Store = 4096,
		StoreBackroom = 8192,
		CustomType = (1 << 31)
	}
}