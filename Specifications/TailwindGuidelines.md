# Tailwind CSS Guidelines

This guideline gives instructions on how to take advantage of Tailwind CSS 4 features.

## Dark Mode

Use themes and layers for dark mode. The following is an example.

```css
@import "tailwindcss";
@custom-variant dark (&:where(.dark, .dark *));   /* §4 */

@theme {
  /* Brand (already in app.css) */
  --color-brand-50:  oklch(0.97 0.02 250);
  --color-brand-500: oklch(0.55 0.18 250);
  --color-brand-700: oklch(0.42 0.16 250);

  /* Surfaces & content */
  --color-surface:        var(--color-white);
  --color-surface-muted:  var(--color-slate-50);
  --color-content:        var(--color-slate-900);
  --color-content-muted:  var(--color-slate-600);
  --color-border-subtle:  var(--color-slate-200);
  --color-ring:           var(--color-brand-500);

  /* Coverage-state accents — bg/text pairs (CDA-UI-05) */
  --color-covered-bg:       var(--color-green-100);
  --color-covered-text:     var(--color-green-800);
  --color-noncovered-bg:    var(--color-red-100);
  --color-noncovered-text:  var(--color-red-800);
  --color-unaddressed-bg:   var(--color-slate-100);
  --color-unaddressed-text: var(--color-slate-700);
  --color-retired-bg:       var(--color-amber-100);
  --color-retired-text:     var(--color-amber-800);
}

@layer base {
  .dark {
    --color-surface:        var(--color-slate-900);
    --color-surface-muted:  var(--color-slate-800);
    --color-content:        var(--color-slate-100);
    --color-content-muted:  var(--color-slate-400);
    --color-border-subtle:  var(--color-slate-700);

    --color-covered-bg:       var(--color-green-950);
    --color-covered-text:     var(--color-green-300);
    --color-noncovered-bg:    var(--color-red-950);
    --color-noncovered-text:  var(--color-red-300);
    --color-unaddressed-bg:   var(--color-slate-800);
    --color-unaddressed-text: var(--color-slate-300);
    --color-retired-bg:       var(--color-amber-950);
    --color-retired-text:     var(--color-amber-300);
  }
}
```

## Use @utility, not @apply

Prefer creating component styling with @utility instead of @appy. @apply is treated as a token replacement when Tailwind compiles so you end up with a lot of duplicate styling in the build artifact. @utility generates actual utility classes that don't duplicate existing classes.

### Rely heavily on defining utility classes for component styling

Taiwind CSS is declarative and the canonical examples show applying several classes on each HTML element. The problem is that you need to repeatedly add the same styling for like components. Navigation links, cards, input styling, etc. When you want to change the look and feel of these components, you must find and replace everywhere. It's error prone and un-maintainable. @utility is the solution that allows you to make changes in one place.

The following is a sample of component stylings.

```css
/* ---------- Surfaces ---------- */
@utility ui-card {
  background-color: var(--color-surface);
  border: 1px solid var(--color-border-subtle);
  color: var(--color-content);
  border-radius: var(--radius-xl);
  padding: calc(var(--spacing) * 5);
  box-shadow: var(--shadow-sm);
}
/* Hover-lift for clickable cards (search results, etc.). Pair: class="ui-card ui-card-interactive" */
@utility ui-card-interactive {
  transition: box-shadow 150ms ease, transform 150ms ease;
  &:hover { box-shadow: var(--shadow-md); transform: translateY(-2px); }
}

/* ---------- Navigation ---------- */
@utility ui-nav-item {
  border-radius: var(--radius-md);
  padding-inline: calc(var(--spacing) * 2);
  padding-block: calc(var(--spacing) * 1);
  font-size: var(--text-sm);
  color: var(--color-content-muted);
  &:hover { color: var(--color-content); background-color: var(--color-surface-muted); }
}
@utility ui-nav-item-active { color: var(--color-brand-700); font-weight: 600; }

/* ---------- Forms ---------- */
@utility ui-label {
  display: block;
  margin-bottom: calc(var(--spacing) * 1);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-content);
}
@utility ui-input {
  display: block;
  width: 100%;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border-subtle);
  background-color: var(--color-surface);
  color: var(--color-content);
  padding-inline: calc(var(--spacing) * 3);
  padding-block: calc(var(--spacing) * 2);
  font-size: var(--text-sm);
  &::placeholder { color: var(--color-content-muted); }
  &:focus {
    outline: 2px solid transparent;   /* keep an outline for forced-colors / high-contrast */
    outline-offset: 2px;
    border-color: var(--color-ring);
    box-shadow: 0 0 0 1px var(--color-ring);
  }
}

/* ---------- Buttons (base + modifiers; use together) ---------- */
@utility ui-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: calc(var(--spacing) * 2);
  border-radius: var(--radius-md);
  padding-inline: calc(var(--spacing) * 4);
  padding-block: calc(var(--spacing) * 2);
  font-size: var(--text-sm);
  font-weight: 500;
  cursor: pointer;
  transition: background-color 150ms ease, opacity 150ms ease;
  &:focus-visible { outline: 2px solid var(--color-ring); outline-offset: 2px; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}
@utility ui-btn-primary {
  background-color: var(--color-brand-500);
  color: var(--color-white);
  &:hover:not(:disabled) { background-color: var(--color-brand-700); }
}
@utility ui-btn-ghost {
  background-color: transparent;
  color: var(--color-content);
  &:hover:not(:disabled) { background-color: var(--color-surface-muted); }
}

/* ---------- Badges (base + coverage-state modifiers) ---------- */
@utility ui-badge {
  display: inline-flex;
  align-items: center;
  gap: calc(var(--spacing) * 1);
  border-radius: 9999px;
  padding-inline: calc(var(--spacing) * 2);
  padding-block: calc(var(--spacing) * 0.5);
  font-size: var(--text-xs);
  font-weight: 600;
}
@utility ui-badge-covered      { background-color: var(--color-covered-bg);     color: var(--color-covered-text); }
@utility ui-badge-noncovered   { background-color: var(--color-noncovered-bg);  color: var(--color-noncovered-text); }
@utility ui-badge-unaddressed  { background-color: var(--color-unaddressed-bg); color: var(--color-unaddressed-text); }
@utility ui-badge-retired      { background-color: var(--color-retired-bg);     color: var(--color-retired-text); }

/* ---------- Citation / inline link ---------- */
@utility ui-link {
  color: var(--color-brand-500);
  text-decoration: underline;
  text-underline-offset: 2px;
  &:hover { color: var(--color-brand-700); }
}
```

When NOT to make a component

- **One-offs** and page-specific spacing/layout → just use utilities in markup.
- A look used **once** → utilities. Promote to a `ui-*` class only when it recurs.
- Keep the catalog **small, named by role, and reviewed** — a sprawling component layer is as hard to maintain as `@apply` soup. Add to it deliberately, not reflexively.