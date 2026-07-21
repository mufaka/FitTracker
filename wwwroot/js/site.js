/**
 * Client-side validation.
 *
 * The browser's Constraint Validation API does the actual checking — the
 * constraints come from Html5ValidationTagHelper, which derives them from the
 * DataAnnotations on the page models. This component only handles the two
 * things the browser cannot do on its own:
 *
 *   1. [Compare] (password confirmation) has no HTML5 equivalent, so it is fed
 *      into the same native validity pipeline via setCustomValidity().
 *   2. Native error bubbles are OS-styled and unstyleable, so messages are
 *      rendered into the existing asp-validation-for spans instead.
 *
 * Messages are read from the data-val-* attributes ASP.NET renders from the
 * model, so the client reports exactly what the server would, and every rule is
 * still declared once — in C#.
 *
 * If this script fails to load the forms keep their native validation, since
 * `novalidate` is applied here rather than in the markup.
 */
document.addEventListener('alpine:init', () => {
    Alpine.data('validatedForm', () => ({
        /** Set once a submit has been attempted; until then errors stay quiet. */
        submitted: false,

        /** Fields the user has actually typed in, keyed by name. */
        dirty: {},

        init() {
            this.$el.setAttribute('novalidate', 'novalidate');
            this.$el.addEventListener('submit', (event) => this.onSubmit(event));

            this.fields().forEach((field) => {
                field.addEventListener('input', () => this.onInput(field));
                field.addEventListener('blur', () => this.onBlur(field));
            });
        },

        /** The inputs ASP.NET flagged for validation. */
        fields() {
            return Array.from(this.$el.querySelectorAll('[data-val="true"][name]'))
                .filter((field) => !field.disabled);
        },

        onSubmit(event) {
            this.fields().forEach((field) => this.syncComparison(field));

            if (this.$el.checkValidity()) {
                return;
            }

            event.preventDefault();
            this.submitted = true;
            this.fields().forEach((field) => this.showMessage(field));
            this.fields().find((field) => !field.validity.valid)?.focus();
        },

        onInput(field) {
            this.dirty[field.name] = true;

            // Editing the password also settles the confirmation field, so every
            // comparison is re-evaluated rather than just this field's own.
            this.fields().forEach((other) => this.syncComparison(other));

            // Only refresh fields that are already showing an error. That lets a
            // message clear the moment it is corrected, without new errors
            // appearing while the user is still part-way through typing.
            this.fields()
                .filter((other) => this.hasMessage(other))
                .forEach((other) => this.showMessage(other));
        },

        onBlur(field) {
            this.syncComparison(field);

            if (this.submitted || this.dirty[field.name]) {
                this.showMessage(field);
            }
        },

        /**
         * Mirrors [Compare] onto the field's custom validity. Both the field to
         * compare against and the message come from the attributes ASP.NET
         * emitted, so nothing about the rule is restated here.
         */
        syncComparison(field) {
            const spec = field.dataset.valEqualtoOther;
            if (!spec) {
                return;
            }

            const other = this.$el.querySelector(
                `[name="${CSS.escape(this.resolveName(field, spec))}"]`
            );
            if (!other) {
                return;
            }

            const message = field.dataset.valEqualto || 'The values do not match.';
            field.setCustomValidity(field.value === other.value ? '' : message);
        },

        /** "*.Password" is relative to the current field's own prefix. */
        resolveName(field, spec) {
            if (!spec.startsWith('*.')) {
                return spec;
            }

            const separator = field.name.lastIndexOf('.');
            return separator === -1
                ? spec.slice(2)
                : `${field.name.slice(0, separator)}.${spec.slice(2)}`;
        },

        /** The asp-validation-for span paired with this field, if any. */
        messageTarget(field) {
            return this.$el.querySelector(`[data-valmsg-for="${CSS.escape(field.name)}"]`);
        },

        hasMessage(field) {
            const target = this.messageTarget(field);
            return target !== null && target.textContent.trim() !== '';
        },

        showMessage(field) {
            const target = this.messageTarget(field);
            if (!target) {
                return;
            }

            const message = field.validity.valid ? '' : this.messageFor(field);
            target.textContent = message;
            target.classList.toggle('field-validation-error', message !== '');
            target.classList.toggle('field-validation-valid', message === '');
        },

        /**
         * Prefer the model's own message over the browser's generic text, so a
         * client-side failure reads identically to the server-side one.
         */
        messageFor(field) {
            const validity = field.validity;
            const data = field.dataset;

            if (validity.customError) {
                return field.validationMessage;
            }
            if (validity.valueMissing) {
                return data.valRequired || field.validationMessage;
            }
            if (validity.typeMismatch) {
                return data.valEmail || data.valUrl || data.valPhone || field.validationMessage;
            }
            if (validity.tooShort || validity.tooLong) {
                return data.valLength || field.validationMessage;
            }
            if (validity.rangeOverflow || validity.rangeUnderflow) {
                return data.valRange || field.validationMessage;
            }
            if (validity.patternMismatch) {
                return data.valRegex || field.validationMessage;
            }

            return field.validationMessage;
        },
    }));
});
