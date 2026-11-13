namespace HearthstoneClone.Core
{
    public enum HeroPowerKind { Fireblast, ArmorUp }
    public static class HeroPower
    {
        public const int Cost = 2;
        public static bool TryUse(Player caster, HeroPowerKind kind, Target target, Board board)
        {
            if(kind!=HeroPowerKind.Fireblast&&kind!=HeroPowerKind.ArmorUp)return false;
            if(caster==null||board==null||(caster!=board.PlayerOne&&caster!=board.PlayerTwo)||caster.Health<=0||caster.CurrentMana<Cost||caster.HasUsedHeroPowerThisTurn)return false;
            if(kind==HeroPowerKind.Fireblast)
            {
                if(target==null||(target.TargetPlayer!=null&&target.TargetMinion!=null))return false;
                if(target.TargetMinion!=null)
                {if(target.TargetMinion.IsDead||board.GetOwnerOf(target.TargetMinion)==null)return false;}
                else if(target.TargetPlayer==null||(target.TargetPlayer!=board.PlayerOne&&target.TargetPlayer!=board.PlayerTwo)||target.TargetPlayer.Health<=0)return false;
            }
            caster.CurrentMana-=Cost;caster.HasUsedHeroPowerThisTurn=true;
            if(kind==HeroPowerKind.ArmorUp)caster.Armor+=2;
            else target.TakeDamage(1);
            return true;
        }
    }
}
