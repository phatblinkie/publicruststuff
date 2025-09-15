using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust;
using UnityEngine;
using System.Reflection;
using Oxide.Core.Libraries.Covalence;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PreventLooting", "CaseMan", "1.16.0", ResourceId = 2469)]
    [Description("Prevent looting by other players")]

    class PreventLooting : RustPlugin
    {	
		#region Variables
	    [PluginReference] Plugin Friends;
		[PluginReference] Plugin ZoneManager;
		[PluginReference] Plugin DynamicPVP;		
		
		bool UsePermission;
		bool UseFriendsAPI;
		bool UseTeams;
		bool AdminCanLoot;
		bool CanAuthCB;
		bool CanLootPl;
		bool CanLootCorpse;
		bool CanLootEnt;
		bool CanRWMount;
		bool CanRWSwap;
		bool CanRWTake;
		bool CanRWUnload;
		bool CanRWLoad;
		bool CanLootBackpack;
		bool CanLootBackpackPlugin;
		bool CanPickup;
		bool CanOvenToggle;
		bool IncludeZoneMode;
		bool UseZoneManager;
		bool UseDynamicPVP;
		bool UseExcludeEntities;
		bool UseCupboard;
		List<object> UseCupboardInclude;
		bool UseOnlyInCupboardRange;		
		List<object> UseOnlyInCupboardRangeInclude;
		bool WipeDetected = false;
		List<object> ZoneID;
		List<object> ExcludeEntities;
		string PLPerm = "preventlooting.use";
		string PlayerPerm = "preventlooting.player";
		string CorpsePerm = "preventlooting.corpse";
		string BackpackPerm = "preventlooting.backpack";
		string StoragePerm = "preventlooting.storage";
		string AdmPerm = "preventlooting.admin";
	
		class StoredData
        {
            public Dictionary<ulong, EntityData> Data = new Dictionary<ulong, EntityData>();
            public StoredData()
            {
            }
        }

        class EntityData
        {
			public List<ulong> Share = new List<ulong>();
			public Dictionary<string, List<ulong>> Quarry = new Dictionary<string, List<ulong>>();
			public EntityData(){}
        }
		
		StoredData storedData;
		
		#endregion
		#region Initialization
		void Init()
        {
            LoadDefaultConfig();
			permission.RegisterPermission(PLPerm, this);
			permission.RegisterPermission(AdmPerm, this);
			permission.RegisterPermission(PlayerPerm, this);
			permission.RegisterPermission(CorpsePerm, this);
			permission.RegisterPermission(BackpackPerm, this);
			permission.RegisterPermission(StoragePerm, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("PreventLooting");
			if (storedData == null) storedData = new StoredData();
			
        }
		void OnServerInitialized()
		{
			if(WipeDetected)		
			{
				PrintWarning("Wipe detected! Clearing all share data!");
				storedData.Data.Clear();
				Interface.Oxide.DataFileSystem.WriteObject("PreventLooting", storedData);
			}	
		}	
		void OnServerSave() => Interface.Oxide.DataFileSystem.WriteObject("PreventLooting", storedData);
		void Unload() => Interface.Oxide.DataFileSystem.WriteObject("PreventLooting", storedData);
		void OnNewSave(string filename) => WipeDetected = true;
		#endregion
		#region Configuration
        protected override void LoadDefaultConfig()
        {
			Config["UsePermission"] = UsePermission = GetConfig("UsePermission", false);
			Config["UseFriendsAPI"] = UseFriendsAPI = GetConfig("UseFriendsAPI", true);
			Config["UseTeams"] = UseTeams = GetConfig("UseTeams", true);
			Config["AdminCanLoot"] = AdminCanLoot = GetConfig("AdminCanLoot", true);
			Config["CanAuthorizeCupboard"] = CanAuthCB = GetConfig("CanAuthorizeCupboard", true);
			Config["CanLootPlayer"] = CanLootPl = GetConfig("CanLootPlayer", false);
			Config["CanLootCorpse"] = CanLootCorpse = GetConfig("CanLootCorpse", false);
			Config["CanLootEntity"] = CanLootEnt = GetConfig("CanLootEntity", false);			
			Config["CanRackedWeaponMount"] = CanRWMount = GetConfig("CanRackedWeaponMount", false);
			Config["CanRackedWeaponSwap"] = CanRWSwap = GetConfig("CanRackedWeaponSwap", false);
			Config["CanRackedWeaponTake"] = CanRWTake = GetConfig("CanRackedWeaponTake", false);
			Config["CanRackedWeaponUnload"] = CanRWUnload = GetConfig("CanRackedWeaponUnload", false);
			Config["CanRackedWeaponLoad"] = CanRWLoad = GetConfig("CanRackedWeaponLoad", false);			
			Config["CanLootBackpack"] = CanLootBackpack = GetConfig("CanLootBackpack", false);
			Config["CanLootBackpackPlugin"] = CanLootBackpackPlugin = GetConfig("CanLootBackpackPlugin", false);
			Config["CanPickup"] = CanPickup = GetConfig("CanPickup", false);
			Config["CanOvenToggle"] = CanOvenToggle = GetConfig("CanOvenToggle", false);
			Config["UseZoneManager"] = UseZoneManager = GetConfig("UseZoneManager", false);
			Config["ZoneManagerIncludeMode"] = IncludeZoneMode = GetConfig("ZoneManagerIncludeMode", false);
			Config["ZoneID"] = ZoneID = GetConfig("ZoneID", new List<object>{"12345678"});
			Config["UseDynamicPVP"] = UseDynamicPVP = GetConfig("UseDynamicPVP", false);
			Config["UseExcludeEntities"] = UseExcludeEntities = GetConfig("UseExcludeEntities", true);
			Config["ExcludeEntities"] = ExcludeEntities = GetConfig("ExcludeEntities", new List<object>{"mailbox.deployed"});
			Config["UseCupboard"] = UseCupboard = GetConfig("UseCupboard", false);
			Config["UseCupboardInclude"] = UseCupboardInclude = GetConfig("UseCupboardInclude", new List<object>{"storage"});
			Config["UseOnlyInCupboardRange"] = UseOnlyInCupboardRange = GetConfig("UseOnlyInCupboardRange", false);
			Config["UseOnlyInCupboardRangeInclude"] = UseOnlyInCupboardRangeInclude = GetConfig("UseOnlyInCupboardRangeInclude", new List<object>{"storage"});

			SaveConfig();
        }		
		#endregion		
		#region Localization
		
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["OnTryLootPlayer"] = "You can not loot players!",
				["OnTryLootCorpse"] = "You can not loot corpses of players!",
				["OnTryLootEntity"] = "You can not use this entity because it is not yours!",
				["OnTryLootWeaponRack"] = "You can not use this weapon rack because it is not yours!",
				["OnTryLootBackpack"] = "You can not open this backpack because it is not yours!",
				["OnTryPickup"] = "You can not pickup this because it is not yours!",
				["NoAccess"] = "This entity is not yours!",
				["PlayerNotFound"] = "Player {0} not found!",
				["ShareAll"] = "All players were given permission to use this entity!",
				["SharePlayer"] = "The player {0} was given permission to use this entity!",
				["NoShare"] = "No permissions have been found for this entity!",
				["ListShare"] = "List of permissions for this entity:",
				["EntityNotFound"] = "You are not standing in front of the entity or away from it!",
				["HasShareAllList"] = "All players are allowed to use this entity!",
				["ShareClear"] = "All permissions for this entity have been deleted!",
				["HasShareAll"] = "All players already have permission to use this entity!",
				["HasSharePlayer"] = "Player {0} already has permission to use this entity!",
				["HasUnShareAll"] = "Permission to use this entity has not been issued to all players!",
				["HasUnSharePlayer"] = "Player {0} has not been granted permission to use this entity!",
				["WasUnShareAll"] = "All players have been removed permission for this entity!",
				["WasUnSharePlayer"] = "The permission to use this entity has been removed from player {0}!",
				["MultiplePlayerFind"]="Multiple players found:",
				["OwnEntity"]="This object is yours!",
				["NoPermission"]="You do not have enough rights to execute this command!",
				["EntPrevent"] = "This entity is protected!",
				["EntNoPrevent"] = "This entity is not protected!",	
				["OnTryOnOff"] = "You can not turn on or off this entity because it is not yours!",
				["OnTryAuthCB"] = "You can not authorize in cupboard because it is not yours!",
            }, this);
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["OnTryLootPlayer"] = "Вы не можете обворовывать игроков!",
                ["OnTryLootCorpse"] = "Вы не можете обворовывать трупы игроков!",
                ["OnTryLootEntity"] = "Вы не можете использовать этот объект, потому что он вам не принадлежит!",
				["OnTryLootWeaponRack"] = "Вы не можете использовать эту оружейную стойку, потому что она вам не принадлежит!",
				["OnTryLootBackpack"] = "Вы не можете открыть чужой рюкзак!",
				["OnTryPickup"] = "Вы не можете взять чужое!",
				["NoAccess"]="Этот объект не принадлежит вам!",
				["PlayerNotFound"]="Игрок с именем {0} не найден!",
				["ShareAll"]="Всем игрокам было выдано разрешение на использование этого объекта!",
				["SharePlayer"]="Игроку {0} было выдано разрешение на использование этого объекта!",
				["NoShare"]="Не найдено разрешений для этого объекта!",
				["ListShare"]="Список разрешений для этого объекта:",
				["EntityNotFound"]="Вы стоите не перед хранилищем или далеко от него!",
				["HasShareAllList"]="Всем игрокам разрешено использовать этот объект!",
				["ShareClear"]="Все разрешения для этого объекта были удалены!",
				["HasShareAll"]="Все игроки уже имеют разрешение на использование этого объекта!",
				["HasSharePlayer"]="Игрок {0} уже имеет разрешение на использование этого объекта!",
				["HasUnShareAll"]="Разрешение на использование этого объекта не было выдано для всех игроков!",
				["HasUnSharePlayer"]="Игроку {0} не было выдано разрешение на использование этого объекта!",	
				["WasUnShareAll"]="Всем игрокам было удалено разрешение на использование этого объекта!",
				["WasUnSharePlayer"]="Игроку {0} было удалено разрешение на использование этого объекта!",
				["MultiplePlayerFind"]="Найдено несколько игроков:",
				["OwnEntity"]="Этот объект принадлежит вам!",
				["NoPermission"]="У вас недостаточно прав для выполнения этой команды!",
				["EntPrevent"]="Этот предмет защищен от воровства!",
				["EntNoPrevent"]="Этот предмет не защищен от воровства!",
				["OnTryOnOff"] = "Вы не можете включить или выключить этот объект, потому что он вам не принадлежит!",
				["OnTryAuthCB"] = "Вы не можете авторизоваться в чужом шкафу, потому что он вам не принадлежит!",
            }, this, "ru");

        }
        #endregion
		#region Hooks
		private object CanLootEntity(BasePlayer player, LootableCorpse corpse)
		{
			if(corpse == null || player == null) return null;
			if(CanLootCorpse) return null;
			if(CheckHelper(player, corpse as BaseEntity)) return null;
			if(IsFriend(corpse.playerSteamID, player.userID)) return null;
			if(UsePermission && !permission.UserHasPermission(corpse.playerSteamID.ToString(), CorpsePerm)) return null;
			if(corpse.playerSteamID < 76561197960265728L || player.userID == corpse.playerSteamID) return null;
			if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(corpse, player)) return null;
			SendReply(player, lang.GetMessage("OnTryLootCorpse", this, player.UserIDString));	
			return true;
		}		
		private void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item.info.itemid == -907422733 || item.info.itemid == 2068884361)
            {
                entity.OwnerID = item.GetOwnerPlayer()?.userID ?? 0;
            }
        }
		private object OnItemPickup(Item item, BasePlayer player)
        {            
			if(item == null || player == null) return null;
			BaseEntity entity = item?.GetWorldEntity();
			if(CheckHelper(player, entity)) return null;
			if(entity.OwnerID != 0 && entity.OwnerID != player.userID && !IsFriend(entity.OwnerID, player.userID)) 
			{	
				if(item.info.itemid == -907422733 || item.info.itemid == 2068884361)
				{
					if(UseCupboard || UseOnlyInCupboardRange)
						if(CheckAuthCupboard(entity, player)) return null;
					SendReply(player, lang.GetMessage("OnTryPickup", this, player.UserIDString));
					return false;
				}
				return null;
			}	
			return null;
        }
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            Item item = entity?.GetItem();	
			if(item == null || player == null) return;
			if(CheckHelper(player, entity)) return;
            if (item.info.itemid == -907422733 || item.info.itemid == 2068884361)
            {
				if(CanLootBackpack) return;
				if(IsFriend(entity.OwnerID, player.userID)) return;
				if(UsePermission && !permission.UserHasPermission(entity.OwnerID.ToString(), BackpackPerm)) return;
				if(entity.OwnerID < 76561197960265728L || player.userID == entity.OwnerID) return;
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(entity, player)) return;
				NextFrame(() =>
					{
                        player.inventory.loot.Clear();
                        player.inventory.loot.SendImmediate();
                    });
				SendReply(player, lang.GetMessage("OnTryLootBackpack", this, player.UserIDString));	
			}
        }		
		private object CanLootEntity(BasePlayer player, DroppedItemContainer container)
		{
			if(container == null || player == null) return null;
			if(CanLootBackpack && CanLootBackpackPlugin) return null;
			if(CheckHelper(player, container as BaseEntity)) return null;
			if(((container as BaseEntity).name.Contains("item_drop_backpack") && !CanLootBackpack) || ((container as BaseEntity).name.Contains("droppedbackpack") && !CanLootBackpackPlugin))
			{
				if(IsFriend(container.playerSteamID, player.userID)) return null;
				if(UsePermission && !permission.UserHasPermission(container.playerSteamID.ToString(), BackpackPerm)) return null;
				if(container.playerSteamID < 76561197960265728L || player.userID == container.playerSteamID) return null;
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(container, player)) return null;
				SendReply(player, lang.GetMessage("OnTryLootBackpack", this, player.UserIDString));	
				return true;
			}
			return null;
		}
		private object CanLootPlayer(BasePlayer target, BasePlayer player)
		{
			if(target == null || player == null) return null;
			if(CanLootPl) return null;
			if(CheckHelper(player, target as BaseEntity)) return null;
			if(IsFriend(target.userID, player.userID)) return null;
			if(UsePermission && !permission.UserHasPermission(target.userID.ToString(), PlayerPerm)) return null;
			if(player.userID == target.userID) return null;
			if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(target, player)) return null;
			SendReply(player, lang.GetMessage("OnTryLootPlayer", this, player.UserIDString));
			return false;
		}	
		private bool CheckHelper(BasePlayer player, BaseEntity entity)
		{
			if(entity == null || player == null) return true;
			if(player.IsAdmin && AdminCanLoot) return true;
			if(permission.UserHasPermission(player.userID.ToString(), AdmPerm)) return true;
			if(UseZoneManager && ZoneManager != null)
			{	
				if(CheckDynamicPVP(player)) return true;
				if((string[])ZoneManager.Call("GetPlayerZoneIDs", player) != null)
				{					
					if(!IncludeZoneMode)
					{					
						foreach(var zoneID in ZoneID)
						{
							if((bool)ZoneManager.Call("isPlayerInZone", zoneID, player)) return true;
						}
					}
					else
					{
						foreach(var zoneID in ZoneID)
						{
							if((bool)ZoneManager.Call("isPlayerInZone", zoneID, player)) return false;			
						}		
						return true;
					}
				}				
			}
			if(entity is SupplyDrop) return true;
			return false;
		}	
		private bool CheckDynamicPVP(BasePlayer player)
        {
			if(UseDynamicPVP && DynamicPVP != null)
			{	
				var Zones = (string[])ZoneManager.Call("GetPlayerZoneIDs", player);
				foreach (var zoneID in Zones)
				{
					if((bool)DynamicPVP.Call("IsDynamicPVPZone", zoneID)) return true;
				}
			}
            return false;
        }		

        private object CanMannequinChangePose(Mannequin mannequin, BasePlayer player)
		{
            if (mannequin == null || player == null) return null;
            if (CanLootEnt) return null;
            BaseEntity entity = mannequin as BaseEntity;
            return xCanLootEntity(player, entity);
		}

		private object CanMannequinSwap(Mannequin mannequin, BasePlayer player)
        {
            if (mannequin == null || player == null) return null;
            if (CanLootEnt) return null;
            BaseEntity entity = mannequin as BaseEntity;
            return xCanLootEntity(player, entity);
        }

		private object CanLootEntity(BasePlayer player, StorageContainer container)
		{
			if(container == null || player == null) return null;
			if(CanLootEnt) return null;
			BaseEntity entity = container as BaseEntity;		
			return xCanLootEntity(player, entity);
		}

		private object CanLootEntity(BasePlayer player, ContainerIOEntity container)
		{
			if(container == null || player == null) return null;
			if(CanLootEnt) return null;
			BaseEntity entity = container as BaseEntity;		
			return xCanLootEntity(player, entity);		
		}
		
		private object CanLootEntity(BasePlayer player, IndustrialEntity container)
		{
			if(container == null || player == null) return null;
			if(CanLootEnt) return null;
			BaseEntity entity = container as BaseEntity;		
			return xCanLootEntity(player, entity);		
		}
		
		private object CanLootEntity(BasePlayer player, WorldItem container)
		{
			if(container == null || player == null) return null;
			if(CanLootEnt) return null;
			BaseEntity entity = container as BaseEntity;		
			return xCanLootEntity(player, entity);		
		}		
					
        private object xCanLootEntity(BasePlayer player, BaseEntity entity)
		{
            //if (entity == null || player == null) return null;
			//if(CanLootEnt) return null;
			if(CheckHelper(player, entity)) return null;
			BaseEntity childentity = entity;
			entity = CheckParent(entity);
			if(entity.OwnerID == player.userID) return null;
			if(UsePermission && !permission.UserHasPermission(entity.OwnerID.ToString(), StoragePerm)) return null;	
			if(UseExcludeEntities)
			{
				if(ExcludeEntities.Contains(entity.ShortPrefabName)) return null;
			}			
			if(IsVendingOpen(player, entity) || IsDropBoxOpen(player, entity)) return null;
			if(IsFriend(entity.OwnerID, player.userID)) return null;
			if(storedData.Data.ContainsKey(entity.net.ID.Value))
			{
				if(childentity == entity)
				{				
					if(storedData.Data[entity.net.ID.Value].Share.Contains(player.userID) || storedData.Data[entity.net.ID.Value].Share.Contains(0)) return null;
				}
				else
				{
					if(storedData.Data[entity.net.ID.Value].Quarry.ContainsKey(childentity.ShortPrefabName))
						if(storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Contains(player.userID) || storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Contains(0)) return null;
				}	
			}
			if(entity.OwnerID != player.userID && entity.OwnerID != 0)
			{			
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(entity, player)) return null;
				SendReply(player, lang.GetMessage("OnTryLootEntity", this, player.UserIDString));
				return false;	
			}
			return null;
		}		
		private bool CheckRackedWeapon(BasePlayer player, BaseEntity entity)
		{			
			if(CheckHelper(player, entity)) return true;
			if(entity.OwnerID == player.userID) return true;
			if(UsePermission && !permission.UserHasPermission(entity.OwnerID.ToString(), StoragePerm)) return true;	
			if(UseExcludeEntities)
			{
				if(ExcludeEntities.Contains(entity.ShortPrefabName)) return true;
			}			
			if(IsFriend(entity.OwnerID, player.userID)) return true;
			if(storedData.Data.ContainsKey(entity.net.ID.Value))
			{			
				if(storedData.Data[entity.net.ID.Value].Share.Contains(player.userID) || storedData.Data[entity.net.ID.Value].Share.Contains(0)) return true;
			}
			if(entity.OwnerID != player.userID && entity.OwnerID != 0)
			{			
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(entity, player)) return true;
				SendReply(player, lang.GetMessage("OnTryLootWeaponRack", this, player.UserIDString));
				return false;	
			}
			return true;
		}	
		private object OnRackedWeaponMount(Item weapon, BasePlayer player, WeaponRack rack)
		{
			if(rack == null || player == null || weapon == null) return null;
			if(CanRWMount) return null;
			BaseEntity entity = rack as BaseEntity;
			if(CheckRackedWeapon(player, entity)) return null;
			return false;
		}
		private object OnRackedWeaponSwap(Item weaponMounting, WeaponRackSlot weaponTaking, BasePlayer player, WeaponRack rack)
		{
			if(rack == null || player == null || weaponMounting == null) return null;
			if(CanRWSwap) return null;
			BaseEntity entity = rack as BaseEntity;
			if(CheckRackedWeapon(player, entity)) return null;
			return false;
		}
		private object OnRackedWeaponTake(Item weapon, BasePlayer player, WeaponRack rack)
		{
			if(rack == null || player == null || weapon == null) return null;
			if(CanRWTake) return null;
			BaseEntity entity = rack as BaseEntity;
			if(CheckRackedWeapon(player, entity)) return null;
			return false;
		}
		private object OnRackedWeaponUnload(Item weapon, BasePlayer player, WeaponRack rack)
		{
			if(rack == null || player == null || weapon == null) return null;
			if(CanRWUnload) return null;
			BaseEntity entity = rack as BaseEntity;
			if(CheckRackedWeapon(player, entity)) return null;
			return false;
		}
		private object OnRackedWeaponLoad(Item weapon, ItemDefinition ammoItem, BasePlayer player, WeaponRack rack)
		{
			if(rack == null || player == null || weapon == null) return null;
			if(CanRWLoad) return null;
			BaseEntity entity = rack as BaseEntity;
			if(CheckRackedWeapon(player, entity)) return null;
			return false;
		}
		private object OnOvenToggle(BaseOven oven, BasePlayer player)
		{
			if(oven == null || player == null) return null;
			if(CanOvenToggle) return null;
			BaseEntity entity = oven as BaseEntity;
			if(CheckHelper(player, entity)) return null;
			if(entity.OwnerID == player.userID) return null;
			if(entity.OwnerID != 0 && entity.OwnerID != player.userID && !IsFriend(entity.OwnerID, player.userID)) 
			{	
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(entity, player)) return null;
				SendReply(player, lang.GetMessage("OnTryOnOff", this, player.UserIDString));
				return false;
			}	
			return null;
		}
		private object CanPickupEntity(BasePlayer player, BaseCombatEntity ent)
		{
			if(ent == null || player == null) return null;
			if(CanPickup) return null;
			BaseEntity entity = ent as BaseEntity;
			if(CheckHelper(player, entity)) return null;
			if(entity.OwnerID != 0 && entity.OwnerID != player.userID && !IsFriend(entity.OwnerID, player.userID)) 
			{	
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(entity, player)) return null;
				SendReply(player, lang.GetMessage("OnTryPickup", this, player.UserIDString));
				return false;
			}	
			return null;
		}
		private object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
		{
			if(CanAuthCB) return null;
			BaseEntity entity = privilege as BaseEntity;
			if(CheckHelper(player, entity)) return null;
			if(entity.OwnerID != 0 && entity.OwnerID != player.userID && !IsFriend(entity.OwnerID, player.userID)) 
			{	
				SendReply(player, lang.GetMessage("OnTryAuthCB", this, player.UserIDString));
				return false;
			}
			return null;
		}
		private BaseEntity CheckParent(BaseEntity entity)
		{
			if(entity.HasParent())
			{
				BaseEntity parententity = entity.GetParentEntity();
				if(parententity is MiningQuarry)	
				{
					entity.OwnerID=parententity.OwnerID;
					entity=parententity;
				}	
			}
			return entity;	
		}
		object CanAdministerVending(BasePlayer player, VendingMachine machine)
		{
			if(machine == null || player == null) return null;
			if(CanLootEnt) return null;
			BaseEntity entity = machine as BaseEntity;
			if(CheckHelper(player, entity)) return null;
			if(UsePermission && !permission.UserHasPermission(entity.OwnerID.ToString(), StoragePerm)) return null;	
			if(entity.OwnerID == player.userID) return null;
			if(UseExcludeEntities)
			{
				if(ExcludeEntities.Contains(entity.ShortPrefabName)) return null;
			}		
			if(IsFriend(entity.OwnerID, player.userID)) return null;
			if(entity.OwnerID != player.userID && entity.OwnerID != 0)
			{			
				if(UseCupboard || UseOnlyInCupboardRange)
					if(CheckAuthCupboard(entity, player)) return null;
				SendReply(player, lang.GetMessage("OnTryLootEntity", this, player.UserIDString));
				return false;	
			}
			return null;
		}
		bool IsVendingOpen(BasePlayer player, BaseEntity entity)
		{
			if(entity is VendingMachine) 
			{	
				VendingMachine shopFront = entity as VendingMachine;
				if(shopFront.PlayerInfront(player)) return true;
				return false;	
			}
			return false;
		}
		bool IsDropBoxOpen(BasePlayer player, BaseEntity entity)
		{
			if(entity is DropBox) 
			{	
				DropBox dropboxFront = entity as DropBox;
				if(dropboxFront.PlayerInfront(player)) return true;
				return false;		
			}
			return false;
		}		
		bool IsFriend(ulong friendid, ulong playerid)
		{
			if(UseFriendsAPI && Friends != null)	
			{
				var fr = Friends.CallHook("AreFriends", friendid, playerid);
                if (fr != null && (bool)fr) return true;
			}
			if(UseTeams)
			{
				BasePlayer player = BasePlayer.FindByID(playerid);
				if(player == null) return false;
				if (player.currentTeam != (long)0)
				{
					RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
					if(playerTeam == null) return false;
					if(playerTeam.members.Contains(friendid)) return true;	
				}		
			}
			return false;
		}		
		bool FindEntityFromRay(BasePlayer player, out object success)
        {
			success = null;
			RaycastHit hit;
			if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 2.2f))
				return false;
			success = hit.GetEntity();
			return true; 
        }
		bool CheckAuthCupboard(object ent, BasePlayer player)
		{		
			BaseEntity entity = ent as BaseEntity;
			ulong ownerid = 0;
			string type = "";
			if(ent is BaseCombatEntity)
				if((ent as BaseCombatEntity).pickup.enabled) type = "pickup";
			if(ent is StorageContainer || ent is ContainerIOEntity || ent is IndustrialCrafter || ent is MiningQuarry || ent is WeaponRack)
			{
				ownerid = entity.OwnerID;
				type = "storage";
			}				
			else if(ent is BasePlayer)
			{
				ownerid = (ent as BasePlayer).userID;
				type = "player";
			}				
			else if(ent is LootableCorpse)
			{
				ownerid = (ent as LootableCorpse).playerSteamID;
				type = "corpse";
			}			
			else if(ent is DroppedItemContainer)
			{
				ownerid = (ent as DroppedItemContainer).playerSteamID;
				if(entity.name.Contains("item_drop_backpack")) type = "backpack";
				else if (entity.name.Contains("droppedbackpack")) type = "backpackplugin";
			}				
			BuildingPrivlidge bprev = player.GetBuildingPrivilege(new OBB(entity.transform.position, entity.transform.rotation, entity.bounds));
			if(UseOnlyInCupboardRangeInclude.Contains(type) && bprev == null)	
			{				
				if(UseOnlyInCupboardRange) return true;
				if(!UseOnlyInCupboardRange) return false;
			}
			if(UseCupboard && UseCupboardInclude.Contains(type) && bprev != null)	
			{
				if(ownerid != 0)
				{
					if(bprev.IsAuthed(player) && bprev.authorizedPlayers.Any<ProtoBuf.PlayerNameID>((ProtoBuf.PlayerNameID x) => x.userid == ownerid)) return true;
				}
				else
				{
					if(bprev.IsAuthed(player)) return true;
				}				
			}								
			return false;
		}
		private IPlayer CheckPlayer(BasePlayer player, string[] args)
		{
			var playerlist = covalence.Players.FindPlayers(args[0]).ToList();
			if(playerlist.Count > 1)
			{
				
				var message="<color=#FF0000>"+lang.GetMessage("MultiplePlayerFind", this, player.UserIDString)+"</color>\n";
				int i=0;
				foreach(var pl in playerlist)
				{
					i++;
					message+= string.Format("{0}. <color=#FFA500>{1}</color> ({2})\n\r", i, pl.Name, pl.Id);
				}
				SendReply(player, message);
                return null;
            }
			var player0 = covalence.Players.FindPlayer(args[0]);
			if(player0==null) 
			{
				SendReply(player, string.Format(lang.GetMessage("PlayerNotFound", this, player.UserIDString), "<color=#FFA500>"+args[0]+"</color>")); 
				return null;
			}
			return player0;
		}	
		#endregion
		#region Commands	
		[ChatCommand("share")]
        void Share(BasePlayer player, string command, string[] args)
        {	
		    if (UsePermission && !permission.UserHasPermission(player.UserIDString, PLPerm)) 
			{
				SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
				return;
			}
			IPlayer player0 = null;
			ulong ID;							
			if (args == null || args.Length <= 0) ID=0;
			else
			{	
				player0 = CheckPlayer(player, args);
				if(player0 == null) return;
				ID=Convert.ToUInt64(player0.Id);
			}
			object success;
			if (FindEntityFromRay(player, out success))
			{
				if (success is StorageContainer || success is ContainerIOEntity || success is IndustrialCrafter)
				{	
					BaseEntity entity = success as BaseEntity;
					BaseEntity childentity = entity;
					entity = CheckParent(entity);
					if(entity.OwnerID == ID)
					{
						SendReply(player, lang.GetMessage("OwnEntity", this, player.UserIDString));
						return;
					}				
					if(entity.OwnerID != player.userID && (!player.IsAdmin || (player.IsAdmin && !AdminCanLoot)))
					{
						SendReply(player, lang.GetMessage("NoAccess", this, player.UserIDString));
						return;
					}
					if(!storedData.Data.ContainsKey(entity.net.ID.Value)) 
					{
						var data = new EntityData();
						storedData.Data.Add(entity.net.ID.Value, data);
						if(childentity != entity) 
						{
							data.Quarry = new Dictionary<string, List<ulong>>();
							data.Quarry.Add(childentity.ShortPrefabName, new List<ulong>{ID});
						}
						else
						{
							data.Share = new List<ulong>();							
							data.Share.Add(ID);
						}
						if(ID==0) SendReply(player, lang.GetMessage("ShareAll", this, player.UserIDString));
						else SendReply(player, string.Format(lang.GetMessage("SharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
					}	
					else 
					{
						if(childentity == entity)
						{
							if(storedData.Data[entity.net.ID.Value].Share.Contains(ID))
							{
								if(ID==0) SendReply(player, lang.GetMessage("HasShareAll", this, player.UserIDString));
								else SendReply(player, string.Format(lang.GetMessage("HasSharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
							}						
							else
							{
								storedData.Data[entity.net.ID.Value].Share.Add(ID);
								if(ID==0) SendReply(player, lang.GetMessage("ShareAll", this, player.UserIDString));
								else SendReply(player, string.Format(lang.GetMessage("SharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
							}
						}
						else
						{
							if(!storedData.Data[entity.net.ID.Value].Quarry.ContainsKey(childentity.ShortPrefabName)) storedData.Data[entity.net.ID.Value].Quarry.Add(childentity.ShortPrefabName, new List<ulong>{});
							if(storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Contains(ID))
							{
								if(ID==0) SendReply(player, lang.GetMessage("HasShareAll", this, player.UserIDString));
								else SendReply(player, string.Format(lang.GetMessage("HasSharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
							}						
							else
							{
								if(storedData.Data[entity.net.ID.Value].Quarry.ContainsKey(childentity.ShortPrefabName)) storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Add(ID);
								else storedData.Data[entity.net.ID.Value].Quarry.Add(childentity.ShortPrefabName, new List<ulong>{ID});
								if(ID==0) SendReply(player, lang.GetMessage("ShareAll", this, player.UserIDString));
								else SendReply(player, string.Format(lang.GetMessage("SharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
							}
						}	
					}
				}
				else
				{
					SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
				}	
		    }
			else
			{
				SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
			}	
        }		
        [ChatCommand("unshare")]
        void Unshare(BasePlayer player, string command, string[] args)
        {
		    if (UsePermission && !permission.UserHasPermission(player.UserIDString, PLPerm)) 
			{
				SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
				return;
			}
			IPlayer player0 = null;
			ulong ID;							
			if (args == null || args.Length <= 0) ID=0;
			else
			{	
				player0 = CheckPlayer(player, args);
				if(player0 == null) return;
				ID=Convert.ToUInt64(player0.Id);
			}
			object success;
			if (FindEntityFromRay(player, out success))			
			{
				if (success is StorageContainer || success is ContainerIOEntity || success is IndustrialCrafter)
				{
					BaseEntity entity = success as BaseEntity;
					BaseEntity childentity = entity;
					entity = CheckParent(entity);
					if(entity.OwnerID != player.userID && (!player.IsAdmin || (player.IsAdmin &&!AdminCanLoot)))
					{
						SendReply(player, lang.GetMessage("NoAccess", this, player.UserIDString));
						return;
					}
					if(!storedData.Data.ContainsKey(entity.net.ID.Value)) 
					{
						SendReply(player, lang.GetMessage("NoShare", this, player.UserIDString));
					}	
					else 
					{
						if(childentity == entity)
						{
							if(!storedData.Data[entity.net.ID.Value].Share.Contains(ID))
							{
								if(ID==0) SendReply(player, lang.GetMessage("HasUnShareAll", this, player.UserIDString));	
								else SendReply(player, string.Format(lang.GetMessage("HasUnSharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));	
							}
							else
							{
								storedData.Data[entity.net.ID.Value].Share.Remove(ID);
								if(storedData.Data[entity.net.ID.Value].Share.Count==0) storedData.Data.Remove(entity.net.ID.Value);
								if(ID==0) SendReply(player, lang.GetMessage("WasUnShareAll", this, player.UserIDString));
								else SendReply(player, string.Format(lang.GetMessage("WasUnSharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
							}
						}
						else
						{
							if(!storedData.Data[entity.net.ID.Value].Quarry.ContainsKey(childentity.ShortPrefabName)) 
							{
								SendReply(player, lang.GetMessage("NoShare", this, player.UserIDString));
								return;
							}
							if(!storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Contains(ID))
							{
								if(ID==0) SendReply(player, lang.GetMessage("HasUnShareAll", this, player.UserIDString));	
								else SendReply(player, string.Format(lang.GetMessage("HasUnSharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));	
							}
							else
							{
								storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Remove(ID);
								if(storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Count==0) storedData.Data[entity.net.ID.Value].Quarry.Remove(childentity.ShortPrefabName);
								if(ID==0) SendReply(player, lang.GetMessage("WasUnShareAll", this, player.UserIDString));
								else SendReply(player, string.Format(lang.GetMessage("WasUnSharePlayer", this, player.UserIDString), "<color=#FFA500>"+player0.Name+"</color>"));
							}
						}
						Sharelist(player);
					}
				}
				else
				{
					SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
				}	
			}	
			else
			{
				SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
			}			
		}		
		[ChatCommand("sharelist")]
        void Sharelist(BasePlayer player)
        {
		    if (UsePermission && !permission.UserHasPermission(player.UserIDString, PLPerm)) 
			{
				SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
				return;
			}			
			object success;
			if (FindEntityFromRay(player, out success))			
			{			
				if (success is StorageContainer || success is ContainerIOEntity || success is IndustrialCrafter)
				{
					BaseEntity entity = success as BaseEntity;
					BaseEntity childentity = entity;
					entity = CheckParent(entity);
					if(entity.OwnerID != player.userID && (!player.IsAdmin || (player.IsAdmin &&!AdminCanLoot)))
					{
						SendReply(player, lang.GetMessage("NoAccess", this, player.UserIDString));
						return;
					}
					if(!storedData.Data.ContainsKey(entity.net.ID.Value)) 
					{
						SendReply(player, lang.GetMessage("NoShare", this, player.UserIDString));
					}
					else
					{
						if(childentity == entity)
						{
							if(storedData.Data[entity.net.ID.Value].Share.Contains(0))
							{
								SendReply(player, lang.GetMessage("HasShareAllList", this, player.UserIDString));
								return;
							}	
							var message="<color=#FFFF00>"+lang.GetMessage("ListShare", this, player.UserIDString)+"</color>\n";
							int i=0;
							foreach(var share in storedData.Data[entity.net.ID.Value].Share)
							{
								i++;
								message+= string.Format("{0}. <color=#00FF00>{1}</color> ({2})\n\r", i, covalence.Players.FindPlayer(share.ToString()).Name, covalence.Players.FindPlayer(share.ToString()).Id);
							}	
							SendReply(player, message);
						}
						else
						{
							if(!storedData.Data[entity.net.ID.Value].Quarry.ContainsKey(childentity.ShortPrefabName)) 
							{
								SendReply(player, lang.GetMessage("NoShare", this, player.UserIDString));
								return;
							}
							if(storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Contains(0))
							{
								SendReply(player, lang.GetMessage("HasShareAllList", this, player.UserIDString));
								return;
							}	
							var message="<color=#FFFF00>"+lang.GetMessage("ListShare", this, player.UserIDString)+"</color>\n";
							int i=0;
							foreach(var share in storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName])
							{
								i++;
								message+= string.Format("{0}. <color=#00FF00>{1}</color> ({2})\n\r", i, covalence.Players.FindPlayer(share.ToString()).Name, covalence.Players.FindPlayer(share.ToString()).Id);
							}	
							SendReply(player, message);
						}	
					}
				}
				else
				{
					SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
				}
			}
			else
			{
				SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
			}
		}		
		[ChatCommand("shareclear")]
        void Shareclear(BasePlayer player)
        {
		    if (UsePermission && !permission.UserHasPermission(player.UserIDString, PLPerm)) 
			{
				SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
				return;
			}			
			object success;
			if (FindEntityFromRay(player, out success))			
			{
				if (success is StorageContainer || success is ContainerIOEntity || success is IndustrialCrafter)
				{
					BaseEntity entity = success as BaseEntity;
					BaseEntity childentity = entity;
					entity = CheckParent(entity);
					if(entity.OwnerID != player.userID && (!player.IsAdmin || (player.IsAdmin &&!AdminCanLoot)))
					{
						SendReply(player, lang.GetMessage("NoAccess", this, player.UserIDString));
						return;
					}
					if(!storedData.Data.ContainsKey(entity.net.ID.Value)) 
					{
						SendReply(player, lang.GetMessage("NoShare", this, player.UserIDString));
					}
					else
					{
						if(childentity == entity)
						{
							storedData.Data[entity.net.ID.Value].Share.Clear();
							if(storedData.Data[entity.net.ID.Value].Share.Count==0) storedData.Data.Remove(entity.net.ID.Value);
							SendReply(player, lang.GetMessage("ShareClear", this, player.UserIDString));
						}
						else
						{
							if(!storedData.Data[entity.net.ID.Value].Quarry.ContainsKey(childentity.ShortPrefabName))
							{
								SendReply(player, lang.GetMessage("NoShare", this, player.UserIDString));
								return;
							}
							storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Clear();
							if(storedData.Data[entity.net.ID.Value].Quarry[childentity.ShortPrefabName].Count==0) storedData.Data[entity.net.ID.Value].Quarry.Remove(childentity.ShortPrefabName);
							SendReply(player, lang.GetMessage("ShareClear", this, player.UserIDString));
						}
					}
				}
				else
				{
					SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
				}				
			}
			else
			{
				SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
			}
		}
		[ChatCommand("checkit")]
        void CheckBP(BasePlayer player)
        {
		    if (UsePermission && !permission.UserHasPermission(player.UserIDString, PLPerm)) 
			{
				SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
				return;
			}			
			object success;
			if (FindEntityFromRay(player, out success))			
			{			
				if (success is StorageContainer || success is ContainerIOEntity || success is IndustrialCrafter)
				{
					BaseEntity entity = success as BaseEntity;
					entity = CheckParent(entity);
					if(entity.OwnerID != player.userID && (!player.IsAdmin || (player.IsAdmin &&!AdminCanLoot)))
					{
						SendReply(player, lang.GetMessage("NoAccess", this, player.UserIDString));
						return;
					}
					if(UseOnlyInCupboardRange)
					{
						BuildingPrivlidge bprev = player.GetBuildingPrivilege(new OBB(entity.transform.position, entity.transform.rotation, entity.bounds));
						if(bprev == null) SendReply(player, "<color=#FF0000>"+lang.GetMessage("EntNoPrevent", this, player.UserIDString)+"</color>\n");
						else SendReply(player, "<color=#CCFF00>"+lang.GetMessage("EntPrevent", this, player.UserIDString)+"</color>\n");		
					}	
					else SendReply(player, "<color=#CCFF00>"+lang.GetMessage("EntPrevent", this, player.UserIDString)+"</color>\n");	
				}
			}
			else
			{
				SendReply(player, lang.GetMessage("EntityNotFound", this, player.UserIDString));
			}
		}
		#endregion
		#region Helpers
		T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T) Convert.ChangeType(Config[name], typeof(T)); 
		#endregion
    }
}
