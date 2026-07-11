using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Features.Evals;

namespace AiPlatform.Infrastructure.Evals;

// ⚠️ APP-SPECIFIC DEMO DATA — KnowledgeBaseSeed-এর উপর ভিত্তি করা eval case। নতুন project-এ বদলে/মুছে ফেলো।
// grounding test: KB-তে থাকা প্রশ্নে সঠিক keyword; off-topic প্রশ্নে refusal।
public class RagEvalDataset : IEvalDataset
{
    public IReadOnlyList<EvalCase> Cases { get; } =
    [
        new("payment", "পেমেন্ট কীভাবে করব?", "hybrid", ["bKash"]),
        new("delivery-charge", "ঢাকার ভেতরে ডেলিভারি চার্জ কত?", "hybrid", ["৬০"]),
        new("warranty", "ইলেকট্রনিক পণ্যে ওয়ারেন্টি কতদিন?", "hybrid", ["১ বছর"]),
        new("return-days", "কত দিনের মধ্যে রিটার্ন করা যায়?", "embedding", ["৭ দিন"]),
        new("off-topic", "আজকে ঢাকার আবহাওয়া কেমন?", "hybrid", [], ExpectRefusal: true),
    ];
}
