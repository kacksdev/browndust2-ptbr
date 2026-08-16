from __future__ import annotations

import json
import re
import sys
import unicodedata
from collections import Counter
from pathlib import Path

from catalog_rules import TEXT_REPLACEMENTS


ROOT = Path(__file__).resolve().parents[2]
CATALOG_DIR = ROOT / "catalog"
REPORT_PATH = ROOT / "catalog" / "audit-report.json"
QUEUE_PATH = ROOT / "work" / "editorial-queue.jsonl"
MARKER_QUEUE_PATH = ROOT / "work" / "marker-failures.jsonl"

TOKEN_PATTERN = re.compile(
    r"("
    r"<\s*/?[A-Za-z][^>]*>|<#[0-9A-Fa-f]{6,8}>"
    r"|<\d+<|>\d+>|\{\d+\{|}\d+}|\[\d+\[|]\d+]|&\d+&"
    r"|\{[^{}]+\}|%[A-Za-z][A-Za-z0-9_]*%|%\d*\$?[A-Za-z]"
    r"|\\[nrt]|\[[0-9][0-9.,%+\- ]*\]|#[0-9A-Fa-f]{6,8}"
    r")"
)
FORMATTING_TOKEN_EXCEPTIONS = {"common|LocalTextTable|1081"}
STORY_TOOLTIP_GODS_LABEL = "common|StoryTooltipTextTable|2"
WORLD_BOOK_KEY_PREFIX = "common|WorldbookTextTable|"
ENGLISH_SIGNAL_EXCEPTIONS = {
    # Pack Jam é o nome próprio do evento, não um fragmento esquecido da
    # tradução. As duas tabelas exibem a mesma mensagem em momentos distintos.
    "common|LocalTextTable|107201",
    "intro|IntroTextTable|107201",
}
ENGLISH_SIGNAL_PATTERN = re.compile(
    r"\b(the|with|from|into|your|you|this|that|loading|settings|equipment|character|support|guild|raid|story|pack|hunt|shop|draw|pickup|enhancement)\b",
    re.IGNORECASE,
)
BROAD_ENGLISH_SIGNAL_PATTERN = re.compile(
    r"\b(battle|mission|event|reward|ally|level|and|have|into|damage|turn|clear|"
    r"from|where|with|was|has|field|hunt|story|pack|shop|outfit|pick-?up)\b",
    re.IGNORECASE,
)
MIXED_LANGUAGE_PATTERN = re.compile(
    r"(?i:\b(of|pick-?up|skins?|gears?|recruit|hunting\s+ground|rewards?|"
    r"floor\s+tiles?|cooldown|buffs?|debuffs?|knockback|fiend|main\s+quest|"
    r"field\s+character|cutscene\s+effect|dialogue\s+head|hair\s+accessory|"
    r"hairstyle|master|miss|lady|magic\s+resistance|chain\s+dmg|"
    r"dmg\s+(?:taken|dealt)|counter\s+buff|search\s+finds?|stealth|"
    r"swiftness|caster|critical\s+hits?|groggy|unguardable|boss|accelerate|"
    r"applying|giggle|chuckle|sigh|huff|sniffle?|pant|gulp|munch|gasp|"
    r"whoosh|swoosh|slurp|whimper|sizzle|clunk|hums|puff|bleat|fiends?|"
    r"homunculi|homunculus|mages?|lobby|rankings?|snacks?|cookies?|hobbies?|"
    r"slots?|show|escape|script|voucher|prompts?|paladin|vassal|checkpoint|"
    r"kernels|archdemon|witch|skewers|cutie|loop|flustered|mutters|flinches|"
    r"flails|pickpocketing|tangy|smug|usual|hits|shell|discount|arduos|duel|"
    r"ditto|frill|bland|piercing|sprint|sparring|crunch|crackle|floor|slide|"
    r"amirite|porridge|logs|completion|roaming|casting|stand|flutter|hardcore|"
    r"performances?|summit|shopping|groove|site|splash|patch(?:es)?|briefing|"
    r"feedback|thud|checkout|cyborgs?|honorable|buffed|gloom|tiebreaker|"
    r"singleplayer|popup|emotes?|bests|kernel|perky|patties|herb|hawser|"
    r"flashy|pubs|chit|purrfect|string|gaudy|wisp|jetskis|ooze|designs|"
    r"cearly|medley|louver|pokes|cushy|sprinters|pull-ups|pennant|snuggly|"
    r"marauders|circlet|dizzier|onesie|lounging|pinwheel|challengers|"
    r"exploits|headbangs|reveal|ditzy|tomboy|vids|tingly|missy|rankers|"
    r"tokens|merch|slyly|eyepatch|backflow|doodle|mech|demoted|zombie|"
    r"fluttering|shrill|yank|go-to|charms|geek|oops|whirrr|ragged|thump|"
    r"patter|tippy-toeing|brainstorming|banners|volley|beachgoers|squishy|"
    r"toppings|racks|blush|frills|wheeze|pouts|appeal|giggles|tang|stomp|"
    r"panting|nitpick|entourage|get-go|salesperson|gravitas|slideshow|"
    r"ballistae|parry|parries|snorts|heavy-duty|boosters|dock|post|clack|"
    r"decks?|login|socket|must|klutz|bullying|lounge|rack|ruckus|beastfolk|"
    r"plop|flush|dodges|homework|reset|know-how|homing|flinch|shows|look|"
    r"grit|foodie|sniping|clatter|yelp|slash|clinking|sliver|frilly|flutters|"
    r"bugging|snarl|crannies|thwack|roleplay|duper|download|plump|multiplayer|"
    r"lobbies|matchmaking|slam|robe|scripts|throb|hack|raise|ghost|sake|woof|"
    r"blob|pitch|peekaboo|standby|pop-ups?|cancel|slient|chipped|"
    r"materiala|game|trade|forward|rapier|sub-vocal|student|ntransfer|"
    r"peep|pranks|getup|plushie|grooving|scoffs|amok|piquing|flashbulbs|"
    r"crafting|smell|food|sprays|hall|building|napping|spot-on|"
    r"thread|masterpiece|sprints|hummed|scalpers|facepalms|whetstone|"
    r"anticlimactic|dragged|drunk|chug|flashbang|moron|slog|curtsy|flap|"
    r"flinching|doodling|gagging|runt|squish|fwoosh|palming|vlog|grilling|"
    r"twinge|pick-me-up|twitch|suds|swish|creak|mussed|rant|giddy|ranting|"
    r"t-shirt|standee|popping|walkie-talkie|winces|freestyle|downloads|"
    r"reroll|headbutt|cockroach|wimp|squeaky|fritters|bromance|hooky|menus|"
    r"tazer|prowl|outsider|pickpocket|patissiers|blabbed|snatch|"
    r"selfies|lookbooks|monopolize|nepo|pushback|baggy|patsy|backburner|"
    r"gatekeepers?|checkpoints|aloof|lovey-dovey|dating|singles|"
    r"threads|boing|poltergeist|proxy|retch|whimpers|hellscape|flinched|"
    r"teetering|thunk|splat|girl|magical|cockpit|doting|menial|back-alley|"
    r"scammer|irritable|darling|ogled|grimaces|pucker|showdown|askew|"
    r"top-notch|stall|heebie-jeebies|gettable|piggyback|stands|"
    r"boogeyman|all-in|winnable|nitpicking|flippant|paladins|sista|"
    r"knickknacks|milady|idly|beastkin|plain-looking|"
    r"skirt-chaser|interlopers|flippantly|slather|snipe|android|"
    r"mongrels|grimmer|skyfaring|slushies|paparazzo|honcho|hogging|"
    r"cowardly|shortcake|cosplaying|guildmaster|hardliner|gateways|looking|"
    r"lowlings|slience|raging|whipped|crouching|hacking|kitchenette|snipers?|"
    r"zapping|addicted|b-brushed|blaming|cawing|handled|merged|overcooked|"
    r"pickiness|punching|racking|request|sibiling|smiling|squishiness|"
    r"swimming|thrilling|untainted|wistfulness|w-wellness|zing|agaaaain|"
    r"alcoh|aliv|angel|anym|aquamarine|argyle|artisan|assult|auct|aven|"
    r"awestruck|b-bony|b-bug|babysitter|backwoods|baguette|beastmen|bersek|"
    r"bipedal|bits|bittersweet|blaster|blizzards|blog|blub|bluffs|c'mere|"
    r"camaraderie|caravel|chirp|chomp|clang|clank|cleeean|clever|clich|"
    r"clingy|common\s+sense|corny|cours|cute|cutieee|d-down|d-dunno|damp|"
    r"dang|deep|depsite|ding|discuss|dolphins|doodles|doppelganger|driver|"
    r"dummy|enchant|enjoyable|exact|finicky|firewall|flabby|"
    r"fluttery|freeloader|friend|fray|fwip|gateway|gack|gblw|"
    r"ghood|ghtly|gorge|graffiti|growl|gruff|grumble|gurgle|h-home|haaand|"
    r"hacker|hair|hardware|heeeere|heeere|helloooo|home|host|hubb|hunk|"
    r"hunter|impress|infect|insights|insult|intercept|ironclad|jiff|jingle|"
    r"juggernaut|kewt|klutzniess|knockbacks|laid-back|layout|legendary|li'l|"
    r"likin|log-in|looks|looot|m-missus|ma'am|magic|marketing|match|"
    r"meee|melee|messy|mimic|minion|mircales|momory|monotone|moreee|mush|"
    r"naaasty|ngth|nihgt|noooise|notenook|novel|offshore|open|ousider|"
    r"overlord|p-p-purple|p-pool|paaain|patheti|pellets|piece|plaaaay|"
    r"playboy|pool|popcorn|pops|poster|posts|pretty|pride|prompty|puffs|purr|"
    r"push-ups?|pushover|puzzle|replay|report|reps|rescue|rolepl|roly-poly|"
    r"rott|rumble|safety|scanner|scrapbooking|secret|selfie|sense|service|"
    r"settle|shake|sherbet|shif|shoo|shuffle|side|skyline|slap|slick|slides|"
    r"slogan|smoothie|snap|snowboard|snug|sound|spam|spark|spells|spire|spit|"
    r"spoilers?|spray|squawk|stat|step|stones|stooore|stream|strike|strip|"
    r"stro|sudsy|sugercoat|sundaes|surf|suspend|talent|temp|tendrils|"
    r"tick|tips|togeth|touchy-touchy|tricky|tropes?|uuuup|"
    r"vagabond|vault|veeeery|veeeil|vieeew|visit|vixen|voice|wakeboard|"
    r"warlocks|whoa|wordy|wrai|wraiths|yeeet|ying|zaaaap|zips|aggro|alias|"
    r"alter|baby|bartender|beeps?|book|caps|checkmate|chubby|clink|close\s+up|"
    r"dragonkin|drizzle|equip|ex-bandit|exquisite|fanfiction|feed|fiendsouls|"
    r"first|fitness|gadgets?|grace|grooowl|jealou|kn-knight|majesty|"
    r"merfolk|moe-ing|noooot|plan|plaster|punks?|revival|save|serious|skewer|"
    r"tender|touch|trance|aka|bed|bokuno|boy|brownie|bukuno|bunker|casty|coy|"
    r"crunchy|curvy|eye|hardwood|him|how|k-kiss|karma|kebabs?|link|maw|"
    r"m+i+l+k|off|paprika|poker|powerrrrr|r-royal|s-sugary-sweet|she|sly|"
    r"snuck|tickle|awa+|wa+a+y|whew|wind|yoo+u|yup|boku|canyons?|maa+ny|one|out|"
    r"sandwich(?:es)?|yoga|zappity|check\s+(?:in|up|out)|flashbacks?|life_end|"
    r"web|jet\s+ski|tickets?|notebooks?|jackpots?|hey|wo+w)\b)"
)
SKILL_ENGLISH_SIGNAL_PATTERN = re.compile(
    r"\b(apply|increase|reduce|attack|attacks|deal|damage|enemy|enemies|"
    r"ally|allies|target|targets|skill|turn|turns|counter|barrier|revive|taunt|"
    r"bleed|burn|rot|poison|silence|frostbite|vulnerability|pressure|summon|"
    r"allied|preemptive|current\s+hp|max\s+hp|magic\s+atk|magic\s+resistance|"
    r"crit\s+rate|crit\s+dmg|property\s+resistance|property\s+dmg|stats?|"
    r"weakening|chains?|stacks?|tiles?|frontmost|cooldown|range\s+attack|"
    r"main\s+target|death|heal|rush|dash|pure|taken|dealt|caster|hits?|boss|"
    r"highest|high|search|stealth|swiftness)\b",
    re.IGNORECASE,
)
UNTRANSLATED_FUNCTION_PATTERN = re.compile(
    r"\b(the|an|and|or|is|are|was|were|be|been|to|of|for|from|with|without|"
    r"in|on|at|by|your|you|this|that|these|those|when|while|after|before|can|"
    r"will|has|have|not|only|all|each|every|into|upon)\b",
    re.IGNORECASE,
)
UNTRANSLATED_CONTENT_PATTERN = re.compile(
    r"\b(use|uses|must|should|obtained|complete|completed|reach|defeat|apply|"
    r"increase|decrease|select|enter|clear|purchase|available|unlock|restore|"
    r"remove|deal|attack|skill|quest|mission|reward|event|battle|story|shop|pack)\b",
    re.IGNORECASE,
)
MACHINE_ARTIFACT_PATTERN = re.compile(
    r"(Ã(?:¡|©|­|³|º|£|µ|§|ª|´)|Â(?: |®|°)|�|"
    r"\bCaracter(?:es)?\b|\bcaracter\b|(?i:\bObjectos?\b|\bFractura\b|"
    r"\bpopuplaridade\b|\bfuging\b|\bTwarted\b|\bRenders\b|"
    r"\bmust-\s*have\b|\bdAmage\b|\bturno\(\s+s\)|"
    r"\bRecolha\s+de\s+trajes\b|\bDescrição\s+da\s+recolha\b|"
    r"\bsorteio\s+da\s+sorte\b|\bMain\s+Missão\b|\bObtível\b|"
    r"\bmal-\s+entendido\b|\bespadasmanship\b|\bVisitas\s+mestrais\b|"
    r"\bItens\s+descascados\b|\bpersonagens\s+coreanos\b|"
    r"\bhabilidades?\s+de\s+habilidade\b|\bPílulas?\s+de\s+Capacidade\b|"
    r"\bde\s+casa\s+de\s+Piso\b|\bbitola\s+roxa\b|\brodeador\b|"
    r"\bcAjude-me\s+a\s+fazê-lo\b|\bdconta\b|\bLogro\b|"
    r"\b\d+(?:st|nd|rd|th)\s+Floor\b))",
)
SKILL_MACHINE_ARTIFACT_PATTERN = re.compile(
    r"(?i:"
    r"derrubá-los\s+de\s+(?:volta|turno)|colidem\s+com\s+a\s+tomada|"
    r"inimiga\s+derrubada|faça\s+um\s+acordo|fazer\s+negócio|\bnegociar\b|"
    r"\bnegociação\b|\bofertas\b|\bFechamento\b|por\s+batimento|"
    r"\b(?:telhas?|telhados?|tilos?|azulejos?)\b|\b1\s+tempo\b|"
    r"\b(?:voltas?|virar)\b|\bMax\s+PV\b|\bPuro\s+DANO\b|"
    r"fraqueza\s+de\s+Stat|\bIsto\s+pode\s+Aumenta\b|"
    r"\bAumentar\s+inimigo\b|por\s+equipe\.\s+em\s+batalha|"
    r"\bSeu\s+<#[^>]+>(?:Taxa\s+CRIT|Resistência\s+Mágica)</color>|"
    r"\bdo\s+do\s+monstro\b|\bDefiend(?:'s)?\b|\bembaixo\b|"
    r"\bpara\s+você\s+mesmo\b|<#[^>]+>danos\s+causados</color>|"
    r"\bReiniciar\b[^.!?]{0,80}\brefrigeração\b|"
    r"\bum\s+<#[^>]+>.{0,48}?</color>\s+(?:Barreira|Vulnerabilidade|Pressão)|"
    r"\bum\s+<#[^>]+>.{0,48}?%</color>\s+chance|"
    r"\b(?:ao\s+inimigo|a\s+si\s+mesmo|aos\s+aliados)\s+"
    r"<#[^>]+>.{0,48}?turno(?:\(s\)|s)?</color>"
    r")"
)

BANNED_FRAGMENTS = (
    "Agora carregamento",
    "AGORA CARREGAMENTO",
    "equipamento de Equipamento Automático",
    "Início Configurações do Menu Cutscene",
    "Pó Marrom 2",
    "Castanho 2",
    "cena de cortes",
    "Conta de Ligação",
    "Borrão de campo",
)


def is_bgm_title(record: dict) -> bool:
    if record.get("scope") != "common" or record.get("table") != "NameTextTable":
        return False
    try:
        record_id = int(record.get("id"))
    except (TypeError, ValueError):
        return False
    return 4_310_000 <= record_id <= 4_339_999


def is_bgm_display_label(record: dict, bgm_sources: set[str]) -> bool:
    if record.get("scope") != "common" or record.get("table") != "LocalTextTable":
        return False
    try:
        record_id = int(record.get("id"))
    except (TypeError, ValueError):
        return False
    return 4_000_000 <= record_id <= 4_399_999 and record.get("source") in bgm_sources


def tokens(text: str) -> list[str]:
    return TOKEN_PATTERN.findall(text or "")


def record_tokens(record: dict, text: str) -> list[str]:
    found = tokens(text)
    if record.get("key") in FORMATTING_TOKEN_EXCEPTIONS:
        return [token for token in found if token not in {r"\n", r"\r", r"\t"}]
    # O cliente aceita o rótulo Gods já resolvido nestas tabelas. Trate o
    # marcador como opcional nos dois lados para que o auditor compare regras
    # equivalentes no catálogo-base (marcador preservado) e na camada revisada
    # (rótulo textual), sem criar uma falha unilateral falsa.
    if record.get("key") == STORY_TOOLTIP_GODS_LABEL:
        return [token for token in found if token != "%Gods%"]
    if str(record.get("key", "")).startswith(WORLD_BOOK_KEY_PREFIX):
        return [token for token in found if token != "%Gods%"]
    return found


def is_probably_untranslated(
    record: dict,
    source: str,
    translation: str,
    protected_bgm: bool = False,
) -> bool:
    if source != translation or protected_bgm:
        return False
    if not re.search(r"[A-Za-z]", source) or len(source.strip()) < 4:
        return False
    return bool(
        UNTRANSLATED_FUNCTION_PATTERN.search(source)
        or UNTRANSLATED_CONTENT_PATTERN.search(source)
    )


def main() -> None:
    # O console padrão do Windows pode usar cp1252 e falhar ao imprimir amostras
    # válidas do catálogo (por exemplo, marcadores de lista como "●").
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    QUEUE_PATH.parent.mkdir(parents=True, exist_ok=True)

    files = sorted(CATALOG_DIR.glob("*.jsonl"), key=lambda p: p.name.casefold())
    if not files:
        raise SystemExit(f"Catálogo público ausente: {CATALOG_DIR}")

    effective: dict[str, dict] = {}
    physical_count = 0
    duplicate_count = 0
    invalid_json: list[dict] = []
    for path in files:
        with path.open("r", encoding="utf-8-sig") as handle:
            for line_number, line in enumerate(handle, 1):
                if not line.strip():
                    continue
                physical_count += 1
                try:
                    record = json.loads(line)
                except json.JSONDecodeError as exc:
                    invalid_json.append({"file": path.name, "line": line_number, "error": str(exc)})
                    continue
                key = record.get("key")
                if key in effective:
                    duplicate_count += 1
                effective[key] = record

    bgm_records = [record for record in effective.values() if is_bgm_title(record)]
    bgm_sources = {record.get("source", "") for record in bgm_records}
    bgm_display_records = [
        record
        for record in effective.values()
        if is_bgm_display_label(record, bgm_sources)
    ]

    empty_translations: list[str] = []
    marker_failures: list[dict] = []
    nfc_failures: list[str] = []
    split_accents: list[dict] = []
    banned_hits: list[dict] = []
    english_signals: list[dict] = []
    broad_english_signals: list[dict] = []
    mixed_language_signals: list[dict] = []
    skill_english_signals: list[dict] = []
    untranslated_signals: list[dict] = []
    machine_artifacts: list[dict] = []
    skill_machine_artifacts: list[dict] = []
    status_counts: Counter[str] = Counter()

    for key, record in effective.items():
        source = record.get("source", "")
        translation = record.get("translation", "")
        searchable_translation = (
            translation.replace(r"\n", "\n")
            .replace(r"\r", "\r")
            .replace(r"\t", "\t")
        )
        lexical_translation = TOKEN_PATTERN.sub(" ", searchable_translation)
        protected_bgm = is_bgm_title(record) or is_bgm_display_label(record, bgm_sources)
        status_counts[record.get("status", "unknown")] += 1
        if source.strip() and not translation.strip():
            empty_translations.append(key)
        source_tokens = record_tokens(record, source)
        translation_tokens = record_tokens(record, translation)
        if source_tokens != translation_tokens:
            marker_failures.append({
                "key": key,
                "source": source,
                "translation": translation,
                "sourceTokens": source_tokens,
                "translationTokens": translation_tokens,
            })
        if unicodedata.normalize("NFC", translation) != translation:
            nfc_failures.append(key)
        split_match = next(
            (broken for broken, _ in TEXT_REPLACEMENTS if broken.casefold() in translation.casefold()),
            None,
        )
        if split_match:
            split_accents.append({"key": key, "match": split_match, "translation": translation})
        for fragment in BANNED_FRAGMENTS:
            if fragment.casefold() in translation.casefold():
                banned_hits.append({"key": key, "fragment": fragment, "translation": translation})
        if (
            translation != source
            and key not in ENGLISH_SIGNAL_EXCEPTIONS
            and ENGLISH_SIGNAL_PATTERN.search(lexical_translation)
        ):
            english_signals.append({"key": key, "source": source, "translation": translation})
        if (
            translation != source
            and key not in ENGLISH_SIGNAL_EXCEPTIONS
            and BROAD_ENGLISH_SIGNAL_PATTERN.search(lexical_translation)
        ):
            broad_english_signals.append({
                "key": key,
                "source": source,
                "translation": translation,
            })
        if not protected_bgm and MIXED_LANGUAGE_PATTERN.search(lexical_translation):
            mixed_language_signals.append({
                "key": key,
                "source": source,
                "translation": translation,
            })
        if (
            translation != source
            and str(record.get("table", "")).startswith("SkillTextTable")
            and SKILL_ENGLISH_SIGNAL_PATTERN.search(lexical_translation)
        ):
            skill_english_signals.append({
                "key": key,
                "source": source,
                "translation": translation,
            })
        if is_probably_untranslated(record, source, translation, protected_bgm):
            untranslated_signals.append({
                "key": key,
                "source": source,
                "translation": translation,
            })
        artifact = MACHINE_ARTIFACT_PATTERN.search(searchable_translation)
        skill_mail_artifact = (
            str(record.get("table", "")).startswith("SkillTextTable")
            and "E-mail" in translation
        )
        if (artifact or skill_mail_artifact) and not protected_bgm:
            machine_artifacts.append({
                "key": key,
                "match": artifact.group(0) if artifact else "E-mail",
                "source": source,
                "translation": translation,
            })
        if str(record.get("table", "")).startswith("SkillTextTable"):
            skill_artifact = SKILL_MACHINE_ARTIFACT_PATTERN.search(searchable_translation)
            if skill_artifact:
                skill_machine_artifacts.append({
                    "key": key,
                    "match": skill_artifact.group(0),
                    "source": source,
                    "translation": translation,
                })

    bgm_changed = [
        {"key": record["key"], "source": record.get("source"), "translation": record.get("translation")}
        for record in bgm_records
        if record.get("translation") != record.get("source")
    ]
    bgm_display_changed = [
        {"key": record["key"], "source": record.get("source"), "translation": record.get("translation")}
        for record in bgm_display_records
        if record.get("translation") != record.get("source")
    ]

    queue = []
    seen: set[str] = set()
    for group, reason in (
        (split_accents, "acentuação separada"),
        (banned_hits, "fragmento proibido"),
        (english_signals, "possível resto de inglês"),
        (broad_english_signals, "resto de inglês na varredura ampliada"),
        (mixed_language_signals, "idioma misto na varredura lexical"),
        (skill_english_signals, "resto de inglês em habilidade"),
        (untranslated_signals, "texto idêntico à fonte com conteúdo traduzível"),
        (machine_artifacts, "artefato provável da tradução automática"),
        (skill_machine_artifacts, "artefato gramatical em habilidade"),
    ):
        for item in group:
            key = item["key"]
            if key in seen:
                continue
            seen.add(key)
            record = effective[key]
            queue.append({
                "key": key,
                "scope": record.get("scope"),
                "table": record.get("table"),
                "id": record.get("id"),
                "source": record.get("source"),
                "translation": record.get("translation"),
                "reason": reason,
            })

    with QUEUE_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        for item in queue:
            handle.write(json.dumps(item, ensure_ascii=False, separators=(",", ":")) + "\n")

    with MARKER_QUEUE_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        for item in marker_failures:
            handle.write(json.dumps(item, ensure_ascii=False, separators=(",", ":")) + "\n")

    hard_failures = (
        len(invalid_json)
        + len(empty_translations)
        + len(marker_failures)
        + len(nfc_failures)
        + len(bgm_changed)
        + len(bgm_display_changed)
        + len(banned_hits)
    )
    report = {
        "catalogFiles": [path.name for path in files],
        "physicalRecords": physical_count,
        "effectiveRecords": len(effective),
        "overrideRecords": duplicate_count,
        "statusCounts": dict(status_counts),
        "invalidJson": len(invalid_json),
        "emptyTranslations": len(empty_translations),
        "markerFailures": len(marker_failures),
        "nfcFailures": len(nfc_failures),
        "bgmTitles": len(bgm_records),
        "bgmTitlesChanged": len(bgm_changed),
        "bgmDisplayLabels": len(bgm_display_records),
        "bgmDisplayLabelsChanged": len(bgm_display_changed),
        "bannedFragments": len(banned_hits),
        "splitAccentCandidates": len(split_accents),
        "englishSignalCandidates": len(english_signals),
        "englishSignalExceptions": len(ENGLISH_SIGNAL_EXCEPTIONS),
        "broadEnglishSignalCandidates": len(broad_english_signals),
        "mixedLanguageCandidates": len(mixed_language_signals),
        "skillEnglishSignalCandidates": len(skill_english_signals),
        "untranslatedCandidates": len(untranslated_signals),
        "machineArtifactCandidates": len(machine_artifacts),
        "skillMachineArtifactCandidates": len(skill_machine_artifacts),
        "editorialQueue": len(queue),
        "hardFailures": hard_failures,
        "readyForNextStage": hard_failures == 0 and len(effective) >= 223_720,
        "editorialReady": len(queue) == 0,
        "samples": {
            "invalidJson": invalid_json[:20],
            "markerFailures": marker_failures[:20],
            "bgmChanged": bgm_changed[:20],
            "bgmDisplayChanged": bgm_display_changed[:20],
            "bannedHits": banned_hits[:20],
            "splitAccents": split_accents[:20],
            "englishSignals": english_signals[:20],
            "broadEnglishSignals": broad_english_signals[:20],
            "mixedLanguageSignals": mixed_language_signals[:20],
            "skillEnglishSignals": skill_english_signals[:20],
            "untranslatedSignals": untranslated_signals[:20],
            "machineArtifacts": machine_artifacts[:20],
            "skillMachineArtifacts": skill_machine_artifacts[:20],
        },
        "queuePath": QUEUE_PATH.relative_to(ROOT).as_posix(),
        "markerQueuePath": MARKER_QUEUE_PATH.relative_to(ROOT).as_posix(),
    }
    REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if hard_failures:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
