using System;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class ADDRESS
	{
		private static readonly ADDRESS[] Table = new ADDRESS[256];

		private static readonly List<ADDRESS> List = new List<ADDRESS>();

		public static readonly ADDRESS INVALID = new ADDRESS(null, "INVALID");

		public static readonly ADDRESS BROADCAST = new ADDRESS(0, "BROADCAST");

		public static readonly ADDRESS ADDRESS_01 = new ADDRESS(1);

		public static readonly ADDRESS ADDRESS_02 = new ADDRESS(2);

		public static readonly ADDRESS ADDRESS_03 = new ADDRESS(3);

		public static readonly ADDRESS ADDRESS_04 = new ADDRESS(4);

		public static readonly ADDRESS ADDRESS_05 = new ADDRESS(5);

		public static readonly ADDRESS ADDRESS_06 = new ADDRESS(6);

		public static readonly ADDRESS ADDRESS_07 = new ADDRESS(7);

		public static readonly ADDRESS ADDRESS_08 = new ADDRESS(8);

		public static readonly ADDRESS ADDRESS_09 = new ADDRESS(9);

		public static readonly ADDRESS ADDRESS_0A = new ADDRESS(10);

		public static readonly ADDRESS ADDRESS_0B = new ADDRESS(11);

		public static readonly ADDRESS ADDRESS_0C = new ADDRESS(12);

		public static readonly ADDRESS ADDRESS_0D = new ADDRESS(13);

		public static readonly ADDRESS ADDRESS_0E = new ADDRESS(14);

		public static readonly ADDRESS ADDRESS_0F = new ADDRESS(15);

		public static readonly ADDRESS ADDRESS_10 = new ADDRESS(16);

		public static readonly ADDRESS ADDRESS_11 = new ADDRESS(17);

		public static readonly ADDRESS ADDRESS_12 = new ADDRESS(18);

		public static readonly ADDRESS ADDRESS_13 = new ADDRESS(19);

		public static readonly ADDRESS ADDRESS_14 = new ADDRESS(20);

		public static readonly ADDRESS ADDRESS_15 = new ADDRESS(21);

		public static readonly ADDRESS ADDRESS_16 = new ADDRESS(22);

		public static readonly ADDRESS ADDRESS_17 = new ADDRESS(23);

		public static readonly ADDRESS ADDRESS_18 = new ADDRESS(24);

		public static readonly ADDRESS ADDRESS_19 = new ADDRESS(25);

		public static readonly ADDRESS ADDRESS_1A = new ADDRESS(26);

		public static readonly ADDRESS ADDRESS_1B = new ADDRESS(27);

		public static readonly ADDRESS ADDRESS_1C = new ADDRESS(28);

		public static readonly ADDRESS ADDRESS_1D = new ADDRESS(29);

		public static readonly ADDRESS ADDRESS_1E = new ADDRESS(30);

		public static readonly ADDRESS ADDRESS_1F = new ADDRESS(31);

		public static readonly ADDRESS ADDRESS_20 = new ADDRESS(32);

		public static readonly ADDRESS ADDRESS_21 = new ADDRESS(33);

		public static readonly ADDRESS ADDRESS_22 = new ADDRESS(34);

		public static readonly ADDRESS ADDRESS_23 = new ADDRESS(35);

		public static readonly ADDRESS ADDRESS_24 = new ADDRESS(36);

		public static readonly ADDRESS ADDRESS_25 = new ADDRESS(37);

		public static readonly ADDRESS ADDRESS_26 = new ADDRESS(38);

		public static readonly ADDRESS ADDRESS_27 = new ADDRESS(39);

		public static readonly ADDRESS ADDRESS_28 = new ADDRESS(40);

		public static readonly ADDRESS ADDRESS_29 = new ADDRESS(41);

		public static readonly ADDRESS ADDRESS_2A = new ADDRESS(42);

		public static readonly ADDRESS ADDRESS_2B = new ADDRESS(43);

		public static readonly ADDRESS ADDRESS_2C = new ADDRESS(44);

		public static readonly ADDRESS ADDRESS_2D = new ADDRESS(45);

		public static readonly ADDRESS ADDRESS_2E = new ADDRESS(46);

		public static readonly ADDRESS ADDRESS_2F = new ADDRESS(47);

		public static readonly ADDRESS ADDRESS_30 = new ADDRESS(48);

		public static readonly ADDRESS ADDRESS_31 = new ADDRESS(49);

		public static readonly ADDRESS ADDRESS_32 = new ADDRESS(50);

		public static readonly ADDRESS ADDRESS_33 = new ADDRESS(51);

		public static readonly ADDRESS ADDRESS_34 = new ADDRESS(52);

		public static readonly ADDRESS ADDRESS_35 = new ADDRESS(53);

		public static readonly ADDRESS ADDRESS_36 = new ADDRESS(54);

		public static readonly ADDRESS ADDRESS_37 = new ADDRESS(55);

		public static readonly ADDRESS ADDRESS_38 = new ADDRESS(56);

		public static readonly ADDRESS ADDRESS_39 = new ADDRESS(57);

		public static readonly ADDRESS ADDRESS_3A = new ADDRESS(58);

		public static readonly ADDRESS ADDRESS_3B = new ADDRESS(59);

		public static readonly ADDRESS ADDRESS_3C = new ADDRESS(60);

		public static readonly ADDRESS ADDRESS_3D = new ADDRESS(61);

		public static readonly ADDRESS ADDRESS_3E = new ADDRESS(62);

		public static readonly ADDRESS ADDRESS_3F = new ADDRESS(63);

		public static readonly ADDRESS ADDRESS_40 = new ADDRESS(64);

		public static readonly ADDRESS ADDRESS_41 = new ADDRESS(65);

		public static readonly ADDRESS ADDRESS_42 = new ADDRESS(66);

		public static readonly ADDRESS ADDRESS_43 = new ADDRESS(67);

		public static readonly ADDRESS ADDRESS_44 = new ADDRESS(68);

		public static readonly ADDRESS ADDRESS_45 = new ADDRESS(69);

		public static readonly ADDRESS ADDRESS_46 = new ADDRESS(70);

		public static readonly ADDRESS ADDRESS_47 = new ADDRESS(71);

		public static readonly ADDRESS ADDRESS_48 = new ADDRESS(72);

		public static readonly ADDRESS ADDRESS_49 = new ADDRESS(73);

		public static readonly ADDRESS ADDRESS_4A = new ADDRESS(74);

		public static readonly ADDRESS ADDRESS_4B = new ADDRESS(75);

		public static readonly ADDRESS ADDRESS_4C = new ADDRESS(76);

		public static readonly ADDRESS ADDRESS_4D = new ADDRESS(77);

		public static readonly ADDRESS ADDRESS_4E = new ADDRESS(78);

		public static readonly ADDRESS ADDRESS_4F = new ADDRESS(79);

		public static readonly ADDRESS ADDRESS_50 = new ADDRESS(80);

		public static readonly ADDRESS ADDRESS_51 = new ADDRESS(81);

		public static readonly ADDRESS ADDRESS_52 = new ADDRESS(82);

		public static readonly ADDRESS ADDRESS_53 = new ADDRESS(83);

		public static readonly ADDRESS ADDRESS_54 = new ADDRESS(84);

		public static readonly ADDRESS ADDRESS_55 = new ADDRESS(85);

		public static readonly ADDRESS ADDRESS_56 = new ADDRESS(86);

		public static readonly ADDRESS ADDRESS_57 = new ADDRESS(87);

		public static readonly ADDRESS ADDRESS_58 = new ADDRESS(88);

		public static readonly ADDRESS ADDRESS_59 = new ADDRESS(89);

		public static readonly ADDRESS ADDRESS_5A = new ADDRESS(90);

		public static readonly ADDRESS ADDRESS_5B = new ADDRESS(91);

		public static readonly ADDRESS ADDRESS_5C = new ADDRESS(92);

		public static readonly ADDRESS ADDRESS_5D = new ADDRESS(93);

		public static readonly ADDRESS ADDRESS_5E = new ADDRESS(94);

		public static readonly ADDRESS ADDRESS_5F = new ADDRESS(95);

		public static readonly ADDRESS ADDRESS_60 = new ADDRESS(96);

		public static readonly ADDRESS ADDRESS_61 = new ADDRESS(97);

		public static readonly ADDRESS ADDRESS_62 = new ADDRESS(98);

		public static readonly ADDRESS ADDRESS_63 = new ADDRESS(99);

		public static readonly ADDRESS ADDRESS_64 = new ADDRESS(100);

		public static readonly ADDRESS ADDRESS_65 = new ADDRESS(101);

		public static readonly ADDRESS ADDRESS_66 = new ADDRESS(102);

		public static readonly ADDRESS ADDRESS_67 = new ADDRESS(103);

		public static readonly ADDRESS ADDRESS_68 = new ADDRESS(104);

		public static readonly ADDRESS ADDRESS_69 = new ADDRESS(105);

		public static readonly ADDRESS ADDRESS_6A = new ADDRESS(106);

		public static readonly ADDRESS ADDRESS_6B = new ADDRESS(107);

		public static readonly ADDRESS ADDRESS_6C = new ADDRESS(108);

		public static readonly ADDRESS ADDRESS_6D = new ADDRESS(109);

		public static readonly ADDRESS ADDRESS_6E = new ADDRESS(110);

		public static readonly ADDRESS ADDRESS_6F = new ADDRESS(111);

		public static readonly ADDRESS ADDRESS_70 = new ADDRESS(112);

		public static readonly ADDRESS ADDRESS_71 = new ADDRESS(113);

		public static readonly ADDRESS ADDRESS_72 = new ADDRESS(114);

		public static readonly ADDRESS ADDRESS_73 = new ADDRESS(115);

		public static readonly ADDRESS ADDRESS_74 = new ADDRESS(116);

		public static readonly ADDRESS ADDRESS_75 = new ADDRESS(117);

		public static readonly ADDRESS ADDRESS_76 = new ADDRESS(118);

		public static readonly ADDRESS ADDRESS_77 = new ADDRESS(119);

		public static readonly ADDRESS ADDRESS_78 = new ADDRESS(120);

		public static readonly ADDRESS ADDRESS_79 = new ADDRESS(121);

		public static readonly ADDRESS ADDRESS_7A = new ADDRESS(122);

		public static readonly ADDRESS ADDRESS_7B = new ADDRESS(123);

		public static readonly ADDRESS ADDRESS_7C = new ADDRESS(124);

		public static readonly ADDRESS ADDRESS_7D = new ADDRESS(125);

		public static readonly ADDRESS ADDRESS_7E = new ADDRESS(126);

		public static readonly ADDRESS ADDRESS_7F = new ADDRESS(127);

		public static readonly ADDRESS ADDRESS_80 = new ADDRESS(128);

		public static readonly ADDRESS ADDRESS_81 = new ADDRESS(129);

		public static readonly ADDRESS ADDRESS_82 = new ADDRESS(130);

		public static readonly ADDRESS ADDRESS_83 = new ADDRESS(131);

		public static readonly ADDRESS ADDRESS_84 = new ADDRESS(132);

		public static readonly ADDRESS ADDRESS_85 = new ADDRESS(133);

		public static readonly ADDRESS ADDRESS_86 = new ADDRESS(134);

		public static readonly ADDRESS ADDRESS_87 = new ADDRESS(135);

		public static readonly ADDRESS ADDRESS_88 = new ADDRESS(136);

		public static readonly ADDRESS ADDRESS_89 = new ADDRESS(137);

		public static readonly ADDRESS ADDRESS_8A = new ADDRESS(138);

		public static readonly ADDRESS ADDRESS_8B = new ADDRESS(139);

		public static readonly ADDRESS ADDRESS_8C = new ADDRESS(140);

		public static readonly ADDRESS ADDRESS_8D = new ADDRESS(141);

		public static readonly ADDRESS ADDRESS_8E = new ADDRESS(142);

		public static readonly ADDRESS ADDRESS_8F = new ADDRESS(143);

		public static readonly ADDRESS ADDRESS_90 = new ADDRESS(144);

		public static readonly ADDRESS ADDRESS_91 = new ADDRESS(145);

		public static readonly ADDRESS ADDRESS_92 = new ADDRESS(146);

		public static readonly ADDRESS ADDRESS_93 = new ADDRESS(147);

		public static readonly ADDRESS ADDRESS_94 = new ADDRESS(148);

		public static readonly ADDRESS ADDRESS_95 = new ADDRESS(149);

		public static readonly ADDRESS ADDRESS_96 = new ADDRESS(150);

		public static readonly ADDRESS ADDRESS_97 = new ADDRESS(151);

		public static readonly ADDRESS ADDRESS_98 = new ADDRESS(152);

		public static readonly ADDRESS ADDRESS_99 = new ADDRESS(153);

		public static readonly ADDRESS ADDRESS_9A = new ADDRESS(154);

		public static readonly ADDRESS ADDRESS_9B = new ADDRESS(155);

		public static readonly ADDRESS ADDRESS_9C = new ADDRESS(156);

		public static readonly ADDRESS ADDRESS_9D = new ADDRESS(157);

		public static readonly ADDRESS ADDRESS_9E = new ADDRESS(158);

		public static readonly ADDRESS ADDRESS_9F = new ADDRESS(159);

		public static readonly ADDRESS ADDRESS_A0 = new ADDRESS(160);

		public static readonly ADDRESS ADDRESS_A1 = new ADDRESS(161);

		public static readonly ADDRESS ADDRESS_A2 = new ADDRESS(162);

		public static readonly ADDRESS ADDRESS_A3 = new ADDRESS(163);

		public static readonly ADDRESS ADDRESS_A4 = new ADDRESS(164);

		public static readonly ADDRESS ADDRESS_A5 = new ADDRESS(165);

		public static readonly ADDRESS ADDRESS_A6 = new ADDRESS(166);

		public static readonly ADDRESS ADDRESS_A7 = new ADDRESS(167);

		public static readonly ADDRESS ADDRESS_A8 = new ADDRESS(168);

		public static readonly ADDRESS ADDRESS_A9 = new ADDRESS(169);

		public static readonly ADDRESS ADDRESS_AA = new ADDRESS(170);

		public static readonly ADDRESS ADDRESS_AB = new ADDRESS(171);

		public static readonly ADDRESS ADDRESS_AC = new ADDRESS(172);

		public static readonly ADDRESS ADDRESS_AD = new ADDRESS(173);

		public static readonly ADDRESS ADDRESS_AE = new ADDRESS(174);

		public static readonly ADDRESS ADDRESS_AF = new ADDRESS(175);

		public static readonly ADDRESS ADDRESS_B0 = new ADDRESS(176);

		public static readonly ADDRESS ADDRESS_B1 = new ADDRESS(177);

		public static readonly ADDRESS ADDRESS_B2 = new ADDRESS(178);

		public static readonly ADDRESS ADDRESS_B3 = new ADDRESS(179);

		public static readonly ADDRESS ADDRESS_B4 = new ADDRESS(180);

		public static readonly ADDRESS ADDRESS_B5 = new ADDRESS(181);

		public static readonly ADDRESS ADDRESS_B6 = new ADDRESS(182);

		public static readonly ADDRESS ADDRESS_B7 = new ADDRESS(183);

		public static readonly ADDRESS ADDRESS_B8 = new ADDRESS(184);

		public static readonly ADDRESS ADDRESS_B9 = new ADDRESS(185);

		public static readonly ADDRESS ADDRESS_BA = new ADDRESS(186);

		public static readonly ADDRESS ADDRESS_BB = new ADDRESS(187);

		public static readonly ADDRESS ADDRESS_BC = new ADDRESS(188);

		public static readonly ADDRESS ADDRESS_BD = new ADDRESS(189);

		public static readonly ADDRESS ADDRESS_BE = new ADDRESS(190);

		public static readonly ADDRESS ADDRESS_BF = new ADDRESS(191);

		public static readonly ADDRESS ADDRESS_C0 = new ADDRESS(192);

		public static readonly ADDRESS ADDRESS_C1 = new ADDRESS(193);

		public static readonly ADDRESS ADDRESS_C2 = new ADDRESS(194);

		public static readonly ADDRESS ADDRESS_C3 = new ADDRESS(195);

		public static readonly ADDRESS ADDRESS_C4 = new ADDRESS(196);

		public static readonly ADDRESS ADDRESS_C5 = new ADDRESS(197);

		public static readonly ADDRESS ADDRESS_C6 = new ADDRESS(198);

		public static readonly ADDRESS ADDRESS_C7 = new ADDRESS(199);

		public static readonly ADDRESS ADDRESS_C8 = new ADDRESS(200);

		public static readonly ADDRESS ADDRESS_C9 = new ADDRESS(201);

		public static readonly ADDRESS ADDRESS_CA = new ADDRESS(202);

		public static readonly ADDRESS ADDRESS_CB = new ADDRESS(203);

		public static readonly ADDRESS ADDRESS_CC = new ADDRESS(204);

		public static readonly ADDRESS ADDRESS_CD = new ADDRESS(205);

		public static readonly ADDRESS ADDRESS_CE = new ADDRESS(206);

		public static readonly ADDRESS ADDRESS_CF = new ADDRESS(207);

		public static readonly ADDRESS ADDRESS_D0 = new ADDRESS(208);

		public static readonly ADDRESS ADDRESS_D1 = new ADDRESS(209);

		public static readonly ADDRESS ADDRESS_D2 = new ADDRESS(210);

		public static readonly ADDRESS ADDRESS_D3 = new ADDRESS(211);

		public static readonly ADDRESS ADDRESS_D4 = new ADDRESS(212);

		public static readonly ADDRESS ADDRESS_D5 = new ADDRESS(213);

		public static readonly ADDRESS ADDRESS_D6 = new ADDRESS(214);

		public static readonly ADDRESS ADDRESS_D7 = new ADDRESS(215);

		public static readonly ADDRESS ADDRESS_D8 = new ADDRESS(216);

		public static readonly ADDRESS ADDRESS_D9 = new ADDRESS(217);

		public static readonly ADDRESS ADDRESS_DA = new ADDRESS(218);

		public static readonly ADDRESS ADDRESS_DB = new ADDRESS(219);

		public static readonly ADDRESS ADDRESS_DC = new ADDRESS(220);

		public static readonly ADDRESS ADDRESS_DD = new ADDRESS(221);

		public static readonly ADDRESS ADDRESS_DE = new ADDRESS(222);

		public static readonly ADDRESS ADDRESS_DF = new ADDRESS(223);

		public static readonly ADDRESS ADDRESS_E0 = new ADDRESS(224);

		public static readonly ADDRESS ADDRESS_E1 = new ADDRESS(225);

		public static readonly ADDRESS ADDRESS_E2 = new ADDRESS(226);

		public static readonly ADDRESS ADDRESS_E3 = new ADDRESS(227);

		public static readonly ADDRESS ADDRESS_E4 = new ADDRESS(228);

		public static readonly ADDRESS ADDRESS_E5 = new ADDRESS(229);

		public static readonly ADDRESS ADDRESS_E6 = new ADDRESS(230);

		public static readonly ADDRESS ADDRESS_E7 = new ADDRESS(231);

		public static readonly ADDRESS ADDRESS_E8 = new ADDRESS(232);

		public static readonly ADDRESS ADDRESS_E9 = new ADDRESS(233);

		public static readonly ADDRESS ADDRESS_EA = new ADDRESS(234);

		public static readonly ADDRESS ADDRESS_EB = new ADDRESS(235);

		public static readonly ADDRESS ADDRESS_EC = new ADDRESS(236);

		public static readonly ADDRESS ADDRESS_ED = new ADDRESS(237);

		public static readonly ADDRESS ADDRESS_EE = new ADDRESS(238);

		public static readonly ADDRESS ADDRESS_EF = new ADDRESS(239);

		public static readonly ADDRESS ADDRESS_F0 = new ADDRESS(240);

		public static readonly ADDRESS ADDRESS_F1 = new ADDRESS(241);

		public static readonly ADDRESS ADDRESS_F2 = new ADDRESS(242);

		public static readonly ADDRESS ADDRESS_F3 = new ADDRESS(243);

		public static readonly ADDRESS ADDRESS_F4 = new ADDRESS(244);

		public static readonly ADDRESS ADDRESS_F5 = new ADDRESS(245);

		public static readonly ADDRESS ADDRESS_F6 = new ADDRESS(246);

		public static readonly ADDRESS ADDRESS_F7 = new ADDRESS(247);

		public static readonly ADDRESS ADDRESS_F8 = new ADDRESS(248);

		public static readonly ADDRESS ADDRESS_F9 = new ADDRESS(249);

		public static readonly ADDRESS ADDRESS_FA = new ADDRESS(250);

		public static readonly ADDRESS ADDRESS_FB = new ADDRESS(251);

		public static readonly ADDRESS ADDRESS_FC = new ADDRESS(252);

		public static readonly ADDRESS ADDRESS_FD = new ADDRESS(253);

		public static readonly ADDRESS ADDRESS_FE = new ADDRESS(254);

		public static readonly ADDRESS ADDRESS_FF = new ADDRESS(byte.MaxValue);

		public readonly byte? Value;

		public readonly string Name;

		public bool IsValidAddress => Value >= 0;

		public bool IsValidDeviceAddress => Value > 0;

		public static IEnumerable<ADDRESS> GetEnumerator()
		{
			return List;
		}

		private ADDRESS(byte value)
			: this(value, value.HexString() + "h")
		{
		}

		private ADDRESS(byte? value, string name)
		{
			Value = value;
			Name = name.Trim();
			if (value.HasValue)
			{
				List.Add(this);
				Table[value.Value] = this;
			}
		}

		public static implicit operator byte(ADDRESS address)
		{
			if (!address.Value.HasValue)
			{
				throw new InvalidCastException("Cannot convert ADDRESS.INVALID to a byte");
			}
			return address.Value.Value;
		}

		public static implicit operator ADDRESS(byte value)
		{
			return Table[value];
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
