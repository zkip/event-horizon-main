//-------------------------------------------------------------------------------
//                                                                               
//    This code was automatically generated.                                     
//    Changes to this file may cause incorrect behavior and will be lost if      
//    the code is regenerated.                                                   
//                                                                               
//-------------------------------------------------------------------------------

using System;
using GameDatabase.Enums;
using GameDatabase.Model;

namespace GameDatabase.Serializable
{
	[Serializable]
	public class MusicPlaylistSerializable : SerializableItem
	{
		public SoundTrackSerializable[] MainMenuMusic;
		public SoundTrackSerializable[] GalaxyMapMusic;
		public SoundTrackSerializable[] CombatMusic;
		public SoundTrackSerializable[] ExplorationMusic;
	}
}
