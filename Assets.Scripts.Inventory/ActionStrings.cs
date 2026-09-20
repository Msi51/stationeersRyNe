using Assets.Scripts.Localization2;
using UnityEngine;

namespace Assets.Scripts.Inventory;

public class ActionStrings : MonoBehaviour
{
	private static readonly int ConsumeFailHash = Animator.StringToHash("ConsumeFail");

	public static readonly int LockHash = 666711088;

	public static readonly int UnlockHash = 1833481689;

	private static readonly int IdleHash = Animator.StringToHash("Idle");

	private static readonly int ActiveHash = Animator.StringToHash("Active");

	private static readonly int RepairHash = Animator.StringToHash("Repair");

	private static readonly int ExtendHash = Animator.StringToHash("Extend");

	private static readonly int RetractHash = Animator.StringToHash("Retract");

	public static string Reload => Localization.GetAction(-1525363869);

	public static string Paint => Localization.GetAction(-1766085869);

	public static string Mine => Localization.GetAction(-912622419);

	public static string Swap => Localization.GetAction(-2053035425);

	public static string Clear => Localization.GetAction(611376642);

	public static string Collect => Localization.GetAction(1807440744);

	public static string Add => Localization.GetAction(-984140537);

	public static string Insert => Localization.GetAction(-1033856061);

	public static string Remove => Localization.GetAction(1865160710);

	public static string Pickup => Localization.GetAction(1177697483);

	public static string Take => Localization.GetAction(-1745895099);

	public static string Build => Localization.GetAction(2086788575);

	public static string Consume => Localization.GetAction(-250289715);

	public static string ConsumeFail => Localization.GetAction(ConsumeFailHash);

	public static string Plant => Localization.GetAction(1791107702);

	public static string Water => Localization.GetAction(988953566);

	public static string Harvest => Localization.GetAction(-117382485);

	public static string Open => Localization.GetAction(71445658);

	public static string Close => Localization.GetAction(-759124288);

	public static string Opened => Localization.GetAction(-1426447763);

	public static string Closed => Localization.GetAction(57362642);

	public static string Off => Localization.GetAction(334568355);

	public static string On => Localization.GetAction(-1674441366);

	public static string Powered => Localization.GetAction(87111221);

	public static string Unpowered => Localization.GetAction(1280144073);

	public static string False => Localization.GetAction(-368294092);

	public static string True => Localization.GetAction(1573839795);

	public static string Construct => Localization.GetAction(-670771122);

	public static string Deconstruct => Localization.GetAction(-2044437770);

	public static string Disconnect => Localization.GetAction(1332168769);

	public static string Connect => Localization.GetAction(-1150107517);

	public static string Load => Localization.GetAction(-2060170461);

	public static string Unload => Localization.GetAction(-809383222);

	public static string GetIn => Localization.GetAction(-130020118);

	public static string Error => Localization.GetAction(-1675848843);

	public static string Lock => Localization.GetAction(LockHash);

	public static string Unlock => Localization.GetAction(UnlockHash);

	public static string Rename => Localization.GetAction(-567193224);

	public static string Up => Localization.GetAction(-703542574);

	public static string Down => Localization.GetAction(-1127399675);

	public static string CallElevator => Localization.GetAction(1395796693);

	public static string Set => Localization.GetAction(-564567236);

	public static string Cycle => Localization.GetAction(1900543639);

	public static string ForceClose => Localization.GetAction(-352037307);

	public static string ForceOpen => Localization.GetAction(-1022792839);

	public static string Vend => Localization.GetAction(815752641);

	public static string Link => Localization.GetAction(-1768016177);

	public static string UnLink => Localization.GetAction(2086213656);

	public static string ForceDefecate => GameStrings.ForceDefecate;

	public static string ForceFeed => Localization.GetAction(1144630390);

	public static string FlipRover => Localization.GetAction(245514190);

	public static string Idle => Localization.GetAction(IdleHash);

	public static string Active => Localization.GetAction(ActiveHash);

	public static string Repair => Localization.GetAction(RepairHash);

	public static string Extend => Localization.GetAction(ExtendHash);

	public static string Retract => Localization.GetAction(RetractHash);
}
