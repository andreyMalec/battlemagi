using System;
using System.Collections.Generic;
using UnityEngine;

public class BookContentController : MonoBehaviour, LanguageAware {
    private TemplateChapterBook _book;
    [SerializeField] private Transform bookmarksParent;
    [SerializeField] private Vector3 bookmarksStep;
    [SerializeField] private GameObject bookmarkPrefab;

    [SerializeField] private Color manaColor;
    [SerializeField] private Color healthColor;

    private readonly Dictionary<string, string> _tooltipById = new(StringComparer.OrdinalIgnoreCase);

    private void Awake() {
        _book = GetComponent<TemplateChapterBook>();
        UpdateContent();
    }

    private string Health(object value) {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(healthColor)}>{value}</color>";
    }

    private string Mana(object value) {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(manaColor)}>{value}</color>";
    }

    private void UpdateContent() {
        bookmarksParent.RemoveChildren();
        var allDefaultSpells = Ctx.GetAllDefaultSpells();
        var document = new TemplateChapterBook.BookDocument();
        var c = 0;
        var s = R.String("second");
        foreach (var archetype in Ctx.Archetypes.archetypes) {
            var chapterIndex = c;
            var chapter = new TemplateChapterBook.BookChapter();
            chapter.icon = Ctx.GetArchetypeIcon(archetype.id);
            chapter.iconTint = archetype.bookColor;

            var archetypeLeftPage = new TemplateChapterBook.BookPage();
            archetypeLeftPage.template = TemplateChapterBook.PageTemplateType.Archetype;
            archetypeLeftPage.image = archetype.bookImage;
            archetypeLeftPage.heading = R.Archetype($"class.name.{archetype.archetypeName}");
            // archetypeLeftPage.paragraph = R.Archetype($"class.description.{archetype.archetypeName}");

            var hp = $"{archetype.maxHealth} +{archetype.healthRegen}/{s}";
            var mp = $"{archetype.maxMana} +{archetype.manaRegen}/{s}";
            archetypeLeftPage.paragraph2 = $"    Health: {Health(hp)}\n" +
                                           $"    Mana: {Mana(mp)}\n" +
                                           $"    Movement speed: {archetype.movementSpeed}";
            chapter.pages.Add(archetypeLeftPage);

            var archetypeRightPage = new TemplateChapterBook.BookPage();
            archetypeRightPage.template = TemplateChapterBook.PageTemplateType.FullText;
            archetypeRightPage.heading = "Passives";
            archetypeRightPage.paragraph = R.Archetype($"class.passives.{archetype.archetypeName}");
            chapter.pages.Add(archetypeRightPage);

            var spells = archetype.spells.Map(s => allDefaultSpells.Find(sp => sp.spell.spellName == s.spellName));

            var i = 0;
            foreach (var spell in spells) {
                var even = i++ % 2 == 0;
                var page = new TemplateChapterBook.BookPage();
                page.template = even
                    ? TemplateChapterBook.PageTemplateType.LeftPage
                    : TemplateChapterBook.PageTemplateType.RightPage;
                page.image = spell.bookImage;
                var cost =
                    $"<color=#{ColorUtility.ToHtmlStringRGB(spell.spell.bloodMagic ? healthColor : manaColor)}>{ManaCost(spell.spell)}</color>";
                page.imageCaption = cost;

                page.heading = R.Spell($"spell.name.{spell.name}");
                page.paragraph = R.Spell($"spell.description.{spell.name}");
                page.paragraph2 = Detail(spell.spell);

                chapter.pages.Add(page);
            }

            if (i % 2 != 0) {
                var empty = new TemplateChapterBook.BookPage();
                chapter.pages.Add(empty);
            }

            document.chapters.Add(chapter);
            var obj = Instantiate(bookmarkPrefab, bookmarksParent.TransformPoint(bookmarksStep * c++),
                Quaternion.identity, bookmarksParent);
            var bookmark = obj.GetComponent<Bookmark>();
            bookmark.Awake();
            bookmark.transform.localRotation = Quaternion.identity;
            bookmark.OnClick += () => _book.MoveToChapter(chapterIndex);
            bookmark.Set(archetype.bookColor, Ctx.GetArchetypeIcon(archetype.id));
        }

        _book.document = document;
    }

    private string Detail(SpellDefinition spell) {
        var tags = SpellTags.Make(spell);

        var plain = R.Spell($"spell.detail.{spell.spellName}");
        var rich = plain;

        foreach (var t in tags) {
            rich = rich.Replace($"[{t.Key}]", t.Value);
        }

        return rich;
    }

    private string ManaCost(SpellDefinition spell) {
        if (!spell.channeling && !spell.charging)
            return $"{spell.manaCost:0}";

        var perSecond = $"{spell.manaPerSecond:0}/{R.String("second")}";
        if (spell.manaCost > 0f && spell.manaPerSecond > 0f)
            return $"{spell.manaCost:0} + {perSecond}";
        if (spell.manaPerSecond > 0f)
            return perSecond;
        return $"{spell.manaCost:0}";
    }

    public void OnLanguageChanged(Language language) {
        UpdateContent();
    }
}