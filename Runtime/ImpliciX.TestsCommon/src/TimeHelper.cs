using System;

namespace ImpliciX.TestsCommon;

public record TimeHelper(TimeSpan Base)
{
    public TimeSpan _0 = TimeSpan.Zero;
    public TimeSpan _1 = Base * 1;
    public TimeSpan _2 = Base * 2;
    public TimeSpan _3 = Base * 3;
    public TimeSpan _4 = Base * 4;
    public TimeSpan _5 = Base * 5;
    public TimeSpan _6 = Base * 6;
    public TimeSpan _7 = Base * 7;
    public TimeSpan _8 = Base * 8;
    public TimeSpan _9 = Base * 9;
    public TimeSpan _10 = Base * 10;
    public TimeSpan _11 = Base * 11;
    public TimeSpan _12 = Base * 12;
    public TimeSpan _13 = Base * 13;
    public TimeSpan _14 = Base * 14;
    public TimeSpan _15 = Base * 15;
    public TimeSpan _16 = Base * 16;
    public TimeSpan _17 = Base * 17;
    public TimeSpan _18 = Base * 18;
    public TimeSpan _19 = Base * 19;
    public TimeSpan _20 = Base * 20;

    public static TimeHelper Minutes() => new (TimeSpan.FromMinutes(1));
}