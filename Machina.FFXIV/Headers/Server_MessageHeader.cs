﻿// Machina.FFXIV ~ Server_MessageHeader.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
using System;
using System.Runtime.InteropServices;


namespace Machina.FFXIV.Headers
{
    /// <summary>
    /// Enumerates the known FFXIV server message types.  Note that some names were adopted from the Sapphire project
    /// </summary>
    public enum Server_MessageType : ushort
    {
        StatusEffectList = 0x263, //0x23c,// 0x0399, X
        BossStatusEffectList = 0x312, //0x0fb,// 0x0236, ?
        Ability1 = 0x2aa, //0x1c6, //0x0165, X
        Ability8 = 0x0b3, //0x2c3, //0x00e9, ?
        Ability16 = 0xe6, //0x2be,// 0x007f, ?
        Ability24 = 0x10a, //0x0076, //0x0299,  ?
        Ability32 = 0x1c8, //0x1ea, //0x01df, ?
        ActorCast = 0x1ec, //0x33e, // 0x028e, X
        AddStatusEffect = 0x10b, //0x25e,// 0x00b9, X
        ActorControl142 = 0x12f, //0x00bc, //0x008d, X
        ActorControl143 = 0x201, //0x02ea, //0x00eb, ?
        ActorControl144 = 0x1be, //0x0109, //0x01f5, ?
        UpdateHpMpTp = 0x075, //0x02cc, //0x012d, X
        PlayerSpawn = 0xdc, //0x01e0, //0x0243, N 
        NpcSpawn = 0x219, //0x0389, //0x021b, N
        NpcSpawn2 = 0x314, //0x31f, //0x0137, N
        ActorMove = 0x1a2, //0x009f, //0x00dd, N
        ActorSetPos = 0x296, //0x0071, //0x0092, N
        ActorGauge = 0x337, //0x2b5, //0x01d2 X
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Server_MessageHeader
    {
        [FieldOffset(0)]
        public uint MessageLength;
        [FieldOffset(4)]
        public uint ActorID;
        [FieldOffset(8)]
        public uint LoginUserID;
        [FieldOffset(12)]
        public uint Unknown1;
        [FieldOffset(16)]
        public ushort Unknown2;
        [FieldOffset(18)]
        public Server_MessageType MessageType;
        [FieldOffset(20)]
        public uint Unknown3;
        [FieldOffset(24)]
        public uint Seconds;
        [FieldOffset(28)]
        public uint Unknown4;
    }    
}