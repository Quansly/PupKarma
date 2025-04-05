using MoreSlugcats;

namespace PupKarma
{
    public static class KarmaPupsMethodsExtend
    {
        public static bool IsSlugpup(this AbstractCreature pup)
        {
            return pup.creatureTemplate.TopAncestor().type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC;
        }

        public static bool HaveOneLowKarmaCapInIteratorRoom(this Oracle iterator)
        {
            for (int i = 0; i < iterator.room.abstractRoom.creatures.Count; i++)
            {
                if (iterator.room.abstractRoom.creatures[i].TryGetPupData(out PupData data) && !data.karmaState.gotIncreaseFromIterators.Contains(iterator.ID.value) && data.karmaCap < 9)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool KarmaGateRequirementPup(this RegionGate gate, KarmaState state)
        {
            if (int.TryParse(gate.karmaRequirements[gate.letThroughDir ? 0 : 1].value, out int karmaGate))
            {
                return karmaGate - 1 <= state.karma;
            }
            return false;
        }

        public static bool HaveKarmaFlower(this Player pup)
        {
            for (int i = 0; i < pup.grasps.Length; i++)
            {
                if (pup.grasps[i] != null && pup.grasps[i].grabbed is KarmaFlower flower && pup.AI.WantsToEatThis(flower))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool PosReadyToSpawnKarmaFlower(this Player player)
        {
            return player.room != null && player.IsTileSolid(1, 0, -1)
                    && !player.room.GetTile(player.bodyChunks[1].pos).DeepWater && !player.IsTileSolid(1, 0, 0) && !player.IsTileSolid(1, 0, 1)
                    && !player.room.GetTile(player.bodyChunks[1].pos).wormGrass && (!player.room.readyForAI ||
                    !player.room.GetTile(player.bodyChunks[1].pos).wormGrass && !player.room.abstractRoom.shelter);
        }

        public static Player GetFirstPlayerOnBack(this Player player)
        {
            while (player.onBack != null)
            {
                player = player.onBack;
            }
            return player;
        }
    }
}
