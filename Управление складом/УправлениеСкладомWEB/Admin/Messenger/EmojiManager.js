/**
 * EmojiManager.js
 * Класс для управления эмодзи в мессенджере
 */

class EmojiManager {
    /**
     * Инициализирует менеджер эмодзи
     * @param {Object} options - Опции инициализации
     */
    static init(options = {}) {
        // Настройки по умолчанию
        this.settings = {
            messageTextAreaId: 'messageTextArea',
            emojiButtonId: 'emojiButton',
            ...options
        };

        // Создаем контейнер для эмодзи-пикера, если его еще нет
        this.createEmojiPicker();
        
        console.log('EmojiManager initialized');
    }

    /**
     * Создает панель выбора эмодзи
     */
    static createEmojiPicker() {
        // Проверяем, существует ли уже панель
        if (document.getElementById('emojiPicker')) {
            return;
        }

        // Создаем HTML структуру эмодзи-пикера
        const pickerHtml = `
            <div id="emojiPicker" class="emoji-picker">
                <div class="emoji-picker-header">
                    <div class="emoji-picker-title">Эмодзи</div>
                    <button class="emoji-picker-close" id="closeEmojiPicker">
                        <i class="ri-close-line"></i>
                    </button>
                </div>
                <div class="emoji-categories">
                    <button class="emoji-category active" data-category="smileys">
                        <i class="ri-emotion-line"></i>
                    </button>
                    <button class="emoji-category" data-category="gestures">
                        <i class="ri-hand-heart-line"></i>
                    </button>
                    <button class="emoji-category" data-category="people">
                        <i class="ri-user-line"></i>
                    </button>
                    <button class="emoji-category" data-category="animals">
                        <i class="ri-bear-smile-line"></i>
                    </button>
                    <button class="emoji-category" data-category="food">
                        <i class="ri-cake-3-line"></i>
                    </button>
                    <button class="emoji-category" data-category="activities">
                        <i class="ri-basketball-line"></i>
                    </button>
                    <button class="emoji-category" data-category="objects">
                        <i class="ri-lightbulb-line"></i>
                    </button>
                </div>
                <div class="emoji-content" id="emojiContent">
                    <!-- Эмодзи будут добавлены через JS -->
                </div>
            </div>
        `;

        // Добавляем пикер в DOM
        document.body.insertAdjacentHTML('beforeend', pickerHtml);

        // Добавляем обработчики событий
        document.getElementById('closeEmojiPicker').addEventListener('click', this.hideEmojiPicker.bind(this));
        
        // Добавляем обработчики для категорий
        const categories = document.querySelectorAll('.emoji-category');
        categories.forEach(category => {
            category.addEventListener('click', () => {
                // Убираем активный класс у всех категорий
                categories.forEach(c => c.classList.remove('active'));
                // Добавляем активный класс к текущей категории
                category.classList.add('active');
                // Загружаем эмодзи для выбранной категории
                this.loadEmojisForCategory(category.dataset.category);
            });
        });

        // Загружаем эмодзи для категории по умолчанию
        this.loadEmojisForCategory('smileys');
    }

    /**
     * Загружает эмодзи для указанной категории
     * @param {string} category - Категория эмодзи
     */
    static loadEmojisForCategory(category) {
        const emojiContent = document.getElementById('emojiContent');
        if (!emojiContent) return;

        // Очищаем контейнер
        emojiContent.innerHTML = '';

        // Получаем эмодзи для выбранной категории
        const emojis = this.getEmojisForCategory(category);

        // Добавляем эмодзи в контейнер
        emojis.forEach(emoji => {
            const emojiElement = document.createElement('div');
            emojiElement.className = 'emoji-item';
            emojiElement.textContent = emoji;
            emojiElement.title = this.getEmojiName(emoji);
            
            // Добавляем обработчик клика для вставки эмодзи
            emojiElement.addEventListener('click', () => {
                this.insertEmoji(emoji);
            });
            
            emojiContent.appendChild(emojiElement);
        });
    }

    /**
     * Возвращает массив эмодзи для указанной категории
     * @param {string} category - Категория эмодзи
     * @returns {Array} - Массив эмодзи
     */
    static getEmojisForCategory(category) {
        const emojiMap = {
            smileys: [
                '😀', '😁', '😂', '🤣', '😃', '😄', '😅', '😆', '😉', '😊',
                '😋', '😎', '😍', '🥰', '😘', '😗', '😙', '😚', '🙂', '🤗',
                '🤩', '🤔', '🤨', '😐', '😑', '😶', '🙄', '😏', '😣', '😥',
                '😮', '🤐', '😯', '😪', '😫', '🥱', '😴', '😌', '😛', '😜',
                '😝', '🤤', '😒', '😓', '😔', '😕', '🙃', '🤑', '😲', '🙁',
                '😖', '😞', '😟', '😤', '😢', '😭', '😦', '😧', '😨', '😩',
                '🤯', '😬', '😰', '😱', '🥵', '🥶', '😳', '🤪', '😵', '🥴'
            ],
            gestures: [
                '👋', '🤚', '🖐️', '✋', '🖖', '👌', '🤌', '🤏', '✌️', '🤞',
                '🤟', '🤘', '🤙', '👈', '👉', '👆', '🖕', '👇', '☝️', '👍',
                '👎', '✊', '👊', '🤛', '🤜', '👏', '🙌', '👐', '🤲', '🤝',
                '🙏', '✍️', '💅', '🤳', '💪', '🦾', '🦿', '🦵', '🦶', '👂',
                '🦻', '👃', '🫀', '🫁', '🧠', '🦷', '🦴', '👀', '👁️', '👅',
                '👄', '💋', '🩸'
            ],
            people: [
                '👶', '🧒', '👦', '👧', '🧑', '👱', '👨', '🧔', '👩', '🧓',
                '👴', '👵', '🙍', '🙎', '🙅', '🙆', '💁', '🙋', '🧏', '🙇',
                '🤦', '🤷', '👮', '🕵️', '💂', '👷', '🤴', '👸', '👳', '👲',
                '🧕', '🤵', '👰', '🤰', '🤱', '👼', '🎅', '🤶', '🦸', '🦹',
                '🧙', '🧚', '🧛', '🧜', '🧝', '🧞', '🧟', '💆', '💇', '🚶',
                '🧍', '🧎', '🏃', '💃', '🕺', '👯', '🧖', '🧗', '🤺', '🏇'
            ],
            animals: [
                '🐶', '🐱', '🐭', '🐹', '🐰', '🦊', '🐻', '🐼', '🐻‍❄️', '🐨',
                '🐯', '🦁', '🐮', '🐷', '🐽', '🐸', '🐵', '🙈', '🙉', '🙊',
                '🐒', '🐔', '🐧', '🐦', '🐤', '🐣', '🐥', '🦆', '🦅', '🦉',
                '🦇', '🐺', '🐗', '🐴', '🦄', '🐝', '🪱', '🐛', '🦋', '🐌',
                '🐞', '🐜', '🪰', '🪲', '🪳', '🦟', '🦗', '🕷️', '🕸️', '🦂',
                '🐢', '🐍', '🦎', '🦖', '🦕', '🐙', '🦑', '🦐', '🦞', '🦀'
            ],
            food: [
                '🍎', '🍐', '🍊', '🍋', '🍌', '🍉', '🍇', '🍓', '🫐', '🍈',
                '🍒', '🍑', '🥭', '🍍', '🥥', '🥝', '🍅', '🍆', '🥑', '🥦',
                '🥬', '🥒', '🌶️', '🫑', '🌽', '🥕', '🧄', '🧅', '🥔', '🍠',
                '🥐', '🥯', '🍞', '🥖', '🥨', '🧀', '🥚', '🍳', '🧈', '🥞',
                '🧇', '🥓', '🥩', '🍗', '🍖', '🦴', '🌭', '🍔', '🍟', '🍕',
                '🫓', '🥪', '🥙', '🧆', '🌮', '🌯', '🫔', '🥗', '🥘', '🫕'
            ],
            activities: [
                '⚽', '🏀', '🏈', '⚾', '🥎', '🎾', '🏐', '🏉', '🥏', '🎱',
                '🪀', '🏓', '🏸', '🏒', '🏑', '🥍', '🏏', '🪃', '🥅', '⛳',
                '🪁', '🎣', '🤿', '🎽', '🎿', '🛷', '🥌', '🎮', '🕹️', '🎲',
                '🎯', '🎭', '🎨', '🎬', '🎤', '🎧', '🎼', '🎹', '🪘', '🥁',
                '🪗', '🎷', '🎺', '🪕', '🎸', '🎻', '🪕', '🥁', '🎬', '🏆',
                '🥇', '🥈', '🥉', '🏅', '🎖️', '🏵️', '🎗️', '🎫', '🎟️', '🎪'
            ],
            objects: [
                '🎭', '👓', '🕶️', '🥽', '🥼', '🦺', '👔', '👕', '👖', '🧣',
                '🧤', '🧥', '🧦', '👗', '👘', '🥻', '🩱', '🩲', '🩳', '👙',
                '👚', '👛', '👜', '👝', '🎒', '🩴', '👞', '👟', '🥾', '🥿',
                '👠', '👡', '🩰', '👢', '👑', '👒', '🎩', '🎓', '🧢', '🪖',
                '⛑️', '📱', '📲', '💻', '⌨️', '🖥️', '🖨️', '🖱️', '🖲️', '🕹️',
                '🗜️', '💽', '💾', '💿', '📀', '📼', '📷', '📸', '📹', '🎥'
            ]
        };

        return emojiMap[category] || [];
    }

    /**
     * Возвращает название эмодзи (упрощенная версия)
     * @param {string} emoji - Эмодзи символ
     * @returns {string} - Название эмодзи
     */
    static getEmojiName(emoji) {
        // В реальном приложении здесь может быть более сложная логика или
        // использование готовой библиотеки для получения названий эмодзи
        return "Эмодзи";
    }

    /**
     * Вставляет эмодзи в текстовое поле
     * @param {string} emoji - Эмодзи для вставки
     */
    static insertEmoji(emoji) {
        const textArea = document.getElementById(this.settings.messageTextAreaId);
        if (!textArea) return;

        // Получаем текущее значение и позиции курсора
        const startPos = textArea.selectionStart;
        const endPos = textArea.selectionEnd;
        const text = textArea.value;

        // Вставляем эмодзи в текущую позицию курсора
        const newText = text.substring(0, startPos) + emoji + text.substring(endPos);
        textArea.value = newText;

        // Устанавливаем курсор после вставленного эмодзи
        const newCursorPos = startPos + emoji.length;
        textArea.setSelectionRange(newCursorPos, newCursorPos);

        // Фокусируемся на текстовом поле
        textArea.focus();
        
        // Имитируем ввод для автоматического изменения высоты (если есть слушатель)
        const inputEvent = new Event('input', { bubbles: true });
        textArea.dispatchEvent(inputEvent);
        
        // Скрываем панель эмодзи после вставки
        this.hideEmojiPicker();
    }

    /**
     * Показывает или скрывает панель эмодзи
     */
    static toggleEmojiPicker() {
        const emojiPicker = document.getElementById('emojiPicker');
        if (!emojiPicker) {
            this.createEmojiPicker();
            setTimeout(() => this.showEmojiPicker(), 10);
            return;
        }

        // Проверяем, видна ли панель
        const isVisible = emojiPicker.classList.contains('active');
        
        if (isVisible) {
            this.hideEmojiPicker();
        } else {
            this.showEmojiPicker();
        }
    }

    /**
     * Показывает панель эмодзи
     */
    static showEmojiPicker() {
        const emojiPicker = document.getElementById('emojiPicker');
        if (!emojiPicker) return;

        // Отображаем панель
        emojiPicker.classList.add('active');
        
        // Добавляем глобальный обработчик для закрытия панели при клике вне её
        setTimeout(() => {
            document.addEventListener('click', this.handleOutsideClick);
        }, 10);
    }

    /**
     * Скрывает панель эмодзи
     */
    static hideEmojiPicker() {
        const emojiPicker = document.getElementById('emojiPicker');
        if (!emojiPicker) return;

        // Скрываем панель
        emojiPicker.classList.remove('active');
        
        // Удаляем глобальный обработчик
        document.removeEventListener('click', this.handleOutsideClick);
    }

    /**
     * Обработчик клика вне панели эмодзи для её закрытия
     * @param {Event} event - Событие клика
     */
    static handleOutsideClick(event) {
        const emojiPicker = document.getElementById('emojiPicker');
        const emojiButton = document.getElementById(EmojiManager.settings.emojiButtonId);
        
        if (!emojiPicker) return;
        
        // Если клик был не по панели и не по кнопке, закрываем панель
        if (!emojiPicker.contains(event.target) && event.target !== emojiButton) {
            EmojiManager.hideEmojiPicker();
        }
    }
} 