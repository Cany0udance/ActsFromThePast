using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast;

public static class ClassicSlimedTracker
{
    public static bool CreatingClassicSlimed { get; set; } = false;

    public static readonly SpireField<CardModel, bool> IsClassicSlimed = new(() => false);
}