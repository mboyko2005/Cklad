/**
 * ImageViewer - класс для просмотра и редактирования изображений в мессенджере
 * Предоставляет функциональность:
 * - Увеличение/уменьшение масштаба
 * - Рисование на изображении
 * - Добавление текста
 * - Сохранение изменений
 */
class ImageViewer {
    constructor(container, imageElement, options = {}) {
        this.container = container;
        this.img = imageElement; // Standardize on img instead of imageElement for consistency
        
        // Настройки по умолчанию
        this.options = {
            containerId: 'imageViewerContainer',
            minScale: 0.1,
            maxScale: 10,
            initialScale: 1,
            enableEditing: true,
            showToolbar: true,
            ...options
        };
        
        // Состояние просмотрщика
        this.isOpen = false;
        this.isEdited = false;
        this.isDrawing = false;
        this.isPanning = false;
        this.isTextMode = false;
        this.isCropMode = false;
        this.scale = this.options.initialScale;
        this.minScale = this.options.minScale;
        this.maxScale = this.options.maxScale;
        this.offsetX = 0;
        this.offsetY = 0;
        this.paths = [];
        this.currentPath = [];
        this.textLayers = [];
        
        // Параметры рисования
        this.currentBrushColor = '#ff0000';
        this.currentBrushWidth = 5;
        
        // Связанные методы для событий
        this._boundHandleMouseDown = null;
        this._boundHandleMouseMove = null;
        this._boundHandleMouseUp = null;
        this._boundHandleWheel = null;
        this._boundHandleDrawStart = null;
        this._boundHandleDrawMove = null;
        this._boundHandleDrawEnd = null;
        
        // Инициализируем просмотрщик
        this._init();
    }

    /**
     * Инициализирует компонент просмотрщика
     * @private
     */
    _init() {
        // Создаем основной контейнер, если его нет
        if (!this.container) {
            this._createViewerContainer();
        }
        
        // Проверяем, загружены ли необходимые библиотеки и загружаем их при необходимости
        this._loadDependencies().then(() => {
            // Если изображение уже загружено, инициализируем canvas
            if (this.img && this.img.complete) {
                this._initCanvas();
            } else if (this.img) {
                // Показываем анимацию загрузки
                this._showLoadingAnimation();
                
                // Если изображение еще не загружено, ждем его загрузки
                this.img.onload = () => {
                    this._hideLoadingAnimation();
                    this._initCanvas();
                    this._initEventListeners();
                    
                    // Анимируем появление изображения
                    this._animateImageAppearance();
                };
                
                // Обработка ошибки загрузки
                this.img.onerror = () => {
                    this._hideLoadingAnimation();
                    this._showErrorMessage('Не удалось загрузить изображение');
                };
            }
        });
    }
    
    /**
     * Загружает необходимые зависимости (библиотеки)
     * @returns {Promise} - Promise, который разрешается, когда все зависимости загружены
     * @private
     */
    _loadDependencies() {
        // Проверяем, есть ли необходимые библиотеки
        const dependencies = [];
        
        // Проверяем наличие anime.js
        if (!window.anime) {
            dependencies.push(this._loadScript('https://cdnjs.cloudflare.com/ajax/libs/animejs/3.2.1/anime.min.js'));
        }
        
        // Проверяем наличие GSAP
        if (!window.gsap) {
            dependencies.push(this._loadScript('https://cdnjs.cloudflare.com/ajax/libs/gsap/3.11.5/gsap.min.js'));
        }
        
        // Загружаем Lottie для анимации загрузки, если его нет
        if (!window.lottie) {
            dependencies.push(this._loadScript('https://cdnjs.cloudflare.com/ajax/libs/bodymovin/5.9.6/lottie.min.js'));
        }
        
        // Если нет зависимостей для загрузки, сразу возвращаем разрешенный Promise
        if (dependencies.length === 0) {
            return Promise.resolve();
        }
        
        // Возвращаем Promise, который разрешается, когда все зависимости загружены
        return Promise.all(dependencies);
    }
    
    /**
     * Загружает скрипт
     * @param {string} src - URL скрипта
     * @returns {Promise} - Promise, который разрешается, когда скрипт загружен
     * @private
     */
    _loadScript(src) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.async = true;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }
    
    /**
     * Показывает анимацию загрузки
     * @private
     */
    _showLoadingAnimation() {
        // Создаем контейнер для анимации загрузки
        const loadingContainer = document.createElement('div');
        loadingContainer.className = 'image-viewer-loading';
        
        // Если загружена библиотека Lottie, используем её
        if (window.lottie) {
            const animationContainer = document.createElement('div');
            animationContainer.className = 'lottie-animation';
            loadingContainer.appendChild(animationContainer);
            
            // Запускаем анимацию загрузки
            window.lottie.loadAnimation({
                container: animationContainer,
                renderer: 'svg',
                loop: true,
                autoplay: true,
                path: 'https://assets1.lottiefiles.com/packages/lf20_usmfx6bp.json' // Анимация загрузки
            });
        } else {
            // Запасной вариант - простой спиннер
            const spinner = document.createElement('div');
            spinner.className = 'loading-spinner';
            for (let i = 0; i < 12; i++) {
                const dot = document.createElement('div');
                dot.className = 'spinner-dot';
                dot.style.transform = `rotate(${i * 30}deg)`;
                dot.style.animationDelay = `${i * 0.1}s`;
                spinner.appendChild(dot);
            }
            loadingContainer.appendChild(spinner);
        }
        
        // Добавляем текст загрузки
        const loadingText = document.createElement('div');
        loadingText.className = 'loading-text';
        loadingText.textContent = 'Загрузка изображения...';
        loadingContainer.appendChild(loadingText);
        
        // Добавляем анимацию загрузки в контейнер
        this.container.appendChild(loadingContainer);
        this.loadingContainer = loadingContainer;
        
        // Добавляем класс active контейнеру, если его нет
        if (!this.container.classList.contains('active')) {
            this.container.classList.add('active');
        }
    }
    
    /**
     * Скрывает анимацию загрузки
     * @private
     */
    _hideLoadingAnimation() {
        if (this.loadingContainer) {
            // Анимируем исчезновение
            if (window.anime) {
                anime({
                    targets: this.loadingContainer,
                    opacity: 0,
                    scale: 0.8,
                    duration: 500,
                    easing: 'easeOutQuad',
                    complete: () => {
                        this.loadingContainer.remove();
                        this.loadingContainer = null;
                    }
                });
            } else {
                // Запасной вариант без anime.js
                this.loadingContainer.style.opacity = '0';
                setTimeout(() => {
                    this.loadingContainer.remove();
                    this.loadingContainer = null;
                }, 500);
            }
        }
    }
    
    /**
     * Показывает сообщение об ошибке
     * @param {string} message - Текст сообщения
     * @private
     */
    _showErrorMessage(message) {
        const errorContainer = document.createElement('div');
        errorContainer.className = 'image-viewer-error';
        
        const errorIcon = document.createElement('div');
        errorIcon.className = 'error-icon';
        errorIcon.innerHTML = '<i class="ri-error-warning-line"></i>';
        errorContainer.appendChild(errorIcon);
        
        const errorText = document.createElement('div');
        errorText.className = 'error-text';
        errorText.textContent = message;
        errorContainer.appendChild(errorText);
        
        // Добавляем кнопку закрытия
        const closeButton = document.createElement('button');
        closeButton.className = 'error-close-btn';
        closeButton.innerHTML = '<i class="ri-close-line"></i>';
        closeButton.addEventListener('click', () => {
            this.close();
        });
        errorContainer.appendChild(closeButton);
        
        // Добавляем сообщение об ошибке в контейнер
        this.container.appendChild(errorContainer);
        
        // Анимируем появление сообщения об ошибке
        if (window.anime) {
            anime({
                targets: errorContainer,
                opacity: [0, 1],
                translateY: [20, 0],
                duration: 500,
                easing: 'easeOutQuad'
            });
        }
    }
    
    /**
     * Анимирует появление изображения
     * @private
     */
    _animateImageAppearance() {
        if (this.canvas) {
            if (window.gsap) {
                gsap.fromTo(this.canvas, 
                    { opacity: 0, scale: 0.9 }, 
                    { opacity: 1, scale: 1, duration: 0.5, ease: "power2.out" }
                );
            } else if (window.anime) {
                anime({
                    targets: this.canvas,
                    opacity: [0, 1],
                    scale: [0.9, 1],
                    duration: 500,
                    easing: 'easeOutQuad'
                });
            } else {
                // Запасной вариант с CSS-анимацией
                this.canvas.classList.add('animate-in');
            }
        }
    }

    /**
     * Создает контейнер для просмотрщика изображений
     * @private
     */
    _createViewerContainer() {
        // Проверяем, существует ли контейнер
        let container = document.getElementById(this.options.containerId || 'imageViewerContainer');
        
        // Если контейнер уже существует, удаляем его содержимое
        if (container) {
            container.innerHTML = '';
        } else {
            container = document.createElement('div');
            container.id = this.options.containerId || 'imageViewerContainer';
            container.className = 'image-viewer';
        }
        
        container.innerHTML = `
            <div class="image-viewer-content">
                <img src="" alt="" class="image-viewer-img">
                <div class="image-viewer-toolbar">
                    <button class="image-viewer-tool toolbar-btn" data-action="zoom-in" title="Увеличить">
                        <i class="ri-zoom-in-line"></i>
                    </button>
                    <button class="image-viewer-tool toolbar-btn" data-action="zoom-out" title="Уменьшить">
                        <i class="ri-zoom-out-line"></i>
                    </button>
                    <button class="image-viewer-tool toolbar-btn" data-action="reset" title="Сбросить масштаб">
                        <i class="ri-fullscreen-line"></i>
                    </button>
                    <div class="toolbar-separator"></div>
                    <button class="image-viewer-tool" id="drawMode" title="Режим рисования">
                        <i class="ri-pencil-line"></i>
                    </button>
                    <button class="image-viewer-tool" id="textMode" title="Добавить текст">
                        <i class="ri-text"></i>
                    </button>
                    <div class="toolbar-separator"></div>
                    <button class="image-viewer-confirm toolbar-btn confirm-edit-btn" data-action="confirm" title="Подтвердить и прикрепить" style="display: none;">
                        <i class="ri-check-line"></i>
                    </button>
                    <button class="image-viewer-tool toolbar-btn" data-action="close" title="Закрыть">
                        <i class="ri-close-line"></i>
                    </button>
                </div>
            </div>
            <div class="drawing-toolbar" style="display: none;">
                <div class="color-picker-container">
                    <input type="color" id="colorPicker" value="${this.currentBrushColor}">
                    <label for="colorPicker" class="color-picker-label" title="Выбрать цвет">
                        <i class="ri-palette-line"></i>
                    </label>
                </div>
                <select id="brushSize" class="brush-size-select" title="Размер кисти">
                    <option value="1">1px</option>
                    <option value="3">3px</option>
                    <option value="5" selected>5px</option>
                    <option value="10">10px</option>
                </select>
                <button class="image-viewer-save-btn" id="saveImage" title="Сохранить изменения">
                    <i class="ri-save-line"></i> Сохранить
                </button>
            </div>
        `;
        
        // Добавляем в DOM, только если его там ещё нет
        if (!document.getElementById(container.id)) {
            document.body.appendChild(container);
        }
        
        this.container = container;
        this.img = container.querySelector('.image-viewer-img');
        
        // Добавляем класс для плавного появления контейнера
        if (window.gsap) {
            gsap.fromTo(container, 
                { opacity: 0 }, 
                { opacity: 1, duration: 0.3, ease: "power1.out" }
            );
        }
    }

    /**
     * Инициализирует обработчики событий
     * @private
     */
    _initEventListeners() {
        if (!this.canvas) return;
        
        // Удаляем предыдущие обработчики для избежания дублирования
        this._removeEventListeners();
        
        // Создаем связанные методы один раз для предотвращения утечек памяти
        if (!this._boundHandleMouseDown) {
            this._boundHandleMouseDown = this._handleMouseDown.bind(this);
            this._boundHandleWheel = this._handleWheel.bind(this);
            this._boundHandleDrawStart = this._handleDrawStart.bind(this);
            this._boundHandleDrawMove = this._handleDrawMove.bind(this);
            this._boundHandleDrawEnd = this._handleDrawEnd.bind(this);
            this._boundHandleMouseMove = this._handleMouseMove.bind(this);
            this._boundHandleMouseUp = this._handleMouseUp.bind(this);
        }
        
        // Обработчики для перетаскивания (перемещения) изображения
        this.canvas.addEventListener('mousedown', this._boundHandleMouseDown);
        document.addEventListener('mousemove', this._boundHandleMouseMove);
        document.addEventListener('mouseup', this._boundHandleMouseUp);
        
        // Обработчик для масштабирования колесиком мыши
        this.canvas.addEventListener('wheel', this._boundHandleWheel);
        
        // Привязываем обработчики кнопок панели инструментов
        const toolbar = this.container.querySelector('.image-viewer-toolbar');
        if (!toolbar) return;
        
        const zoomInBtn = toolbar.querySelector('.toolbar-btn[data-action="zoom-in"]');
        const zoomOutBtn = toolbar.querySelector('.toolbar-btn[data-action="zoom-out"]');
        const resetZoomBtn = toolbar.querySelector('.toolbar-btn[data-action="reset"]');
        const closeBtn = toolbar.querySelector('.toolbar-btn[data-action="close"]');
        const confirmBtn = toolbar.querySelector('.toolbar-btn[data-action="confirm"]');
        
        if (zoomInBtn) {
            // Заменяем обработчик, чтобы применять одиночное изменение масштаба за клик
            zoomInBtn.addEventListener('click', () => {
                // Применяем фиксированное увеличение за один клик
                const newScale = Math.min(this.scale * 1.2, this.maxScale);
                
                // Рассчитываем центр видимой области
                const centerX = this.canvas.width / 2;
                const centerY = this.canvas.height / 2;
                
                // Рассчитываем новые смещения, чтобы сохранить центр
                const offsetX = centerX - (centerX - this.offsetX) * (newScale / this.scale);
                const offsetY = centerY - (centerY - this.offsetY) * (newScale / this.scale);
                
                if (window.gsap) {
                    gsap.to(this, {
                        scale: newScale,
                        offsetX: offsetX,
                        offsetY: offsetY,
                        duration: 0.3,
                        ease: "power2.out",
                        onUpdate: () => this._updateCanvas()
                    });
                } else {
                    this.scale = newScale;
                    this.offsetX = offsetX;
                    this.offsetY = offsetY;
                    this._updateCanvas();
                }
                console.log('Zoom in clicked, new scale:', this.scale);
            });
        }
        
        if (zoomOutBtn) {
            // Заменяем обработчик, чтобы применять одиночное изменение масштаба за клик
            zoomOutBtn.addEventListener('click', () => {
                // Применяем фиксированное уменьшение за один клик
                const newScale = Math.max(this.scale * 0.8, this.minScale);
                
                // Рассчитываем центр видимой области
                const centerX = this.canvas.width / 2;
                const centerY = this.canvas.height / 2;
                
                // Рассчитываем новые смещения, чтобы сохранить центр
                const offsetX = centerX - (centerX - this.offsetX) * (newScale / this.scale);
                const offsetY = centerY - (centerY - this.offsetY) * (newScale / this.scale);
                
                if (window.gsap) {
                    gsap.to(this, {
                        scale: newScale,
                        offsetX: offsetX,
                        offsetY: offsetY,
                        duration: 0.3,
                        ease: "power2.out",
                        onUpdate: () => this._updateCanvas()
                    });
                } else {
                    this.scale = newScale;
                    this.offsetX = offsetX;
                    this.offsetY = offsetY;
                    this._updateCanvas();
                }
                console.log('Zoom out clicked, new scale:', this.scale);
            });
        }
        
        if (resetZoomBtn) {
            resetZoomBtn.addEventListener('click', () => {
                this._resetZoom();
                console.log('Reset zoom clicked, new scale:', this.scale);
            });
        }
        
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                this.close();
                if (typeof this.onClose === 'function') {
                    this.onClose();
                }
            });
        }
        
        if (confirmBtn) {
            confirmBtn.addEventListener('click', this._confirmEdit.bind(this));
        }
        
        // Обработчики для режима рисования
        const drawModeBtn = toolbar.querySelector('#drawMode');
        if (drawModeBtn) drawModeBtn.addEventListener('click', this._enableDrawMode.bind(this));
        
        // Обработчики для режима текста
        const textModeBtn = toolbar.querySelector('#textMode');
        if (textModeBtn) textModeBtn.addEventListener('click', this._enableTextMode.bind(this));
        
        // Обработчики для рисования на canvas
        this.canvas.addEventListener('mousedown', this._boundHandleDrawStart);
        this.canvas.addEventListener('mousemove', this._boundHandleDrawMove);
        this.canvas.addEventListener('mouseup', this._boundHandleDrawEnd);
        this.canvas.addEventListener('mouseout', this._boundHandleDrawEnd);
    }

    /**
     * Обрабатывает нажатие кнопки мыши для рисования
     * @param {MouseEvent} event - Событие мыши
     * @private
     */
    _handleDrawStart(event) {
        // Проверяем, что мы в режиме рисования
        if (!this.isDrawing) return;
        
        // Предотвращаем перетаскивание в режиме рисования
        event.preventDefault();
        event.stopPropagation();
        
        this.drawingStarted = true;
        this.currentPath = [];
        
        const rect = this.canvas.getBoundingClientRect();
        
        // Корректируем координаты с учетом масштаба и позиции
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        
        const x = (event.clientX - rect.left) * scaleX;
        const y = (event.clientY - rect.top) * scaleY;
        
        // Устанавливаем параметры рисования
        this.ctx.beginPath();
        this.ctx.moveTo(x, y);
        this.ctx.lineWidth = this.currentBrushWidth;
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';
        this.ctx.strokeStyle = this.currentBrushColor;
        
        // Сохраняем точку в пути
        this.currentPath.push({ x, y });
        
        console.log('Drawing started at', x, y);
    }
    
    /**
     * Обрабатывает перемещение мыши для рисования
     * @param {MouseEvent} event - Событие мыши
     * @private
     */
    _handleDrawMove(event) {
        if (!this.isDrawing || !this.drawingStarted) return;
        
        // Предотвращаем действия по умолчанию
        event.preventDefault();
        event.stopPropagation();
        
        const rect = this.canvas.getBoundingClientRect();
        
        // Корректируем координаты с учетом масштаба и позиции
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        
        const x = (event.clientX - rect.left) * scaleX;
        const y = (event.clientY - rect.top) * scaleY;
        
        // Рисуем линию до новой точки
        this.ctx.lineTo(x, y);
        this.ctx.stroke();
        
        // Продолжаем путь от новой точки без начала нового пути
        // Убираем начало нового пути, так как это прерывает непрерывное рисование
        // this.ctx.beginPath();
        // this.ctx.moveTo(x, y);
        
        // Сохраняем точку в пути
        this.currentPath.push({ x, y });
        
        // Отмечаем, что изображение было отредактировано
        this.isEdited = true;
    }
    
    /**
     * Обрабатывает отпускание кнопки мыши для завершения рисования
     * @param {MouseEvent} event - Событие мыши
     * @private
     */
    _handleDrawEnd(event) {
        if (!this.isDrawing || !this.drawingStarted) return;
        
        this.drawingStarted = false;
        
        // Завершаем путь
        this.ctx.closePath();
        
        // Сохраняем нарисованный путь
        if (this.currentPath.length > 0) {
            this.paths.push({
                path: [...this.currentPath], // Создаем копию массива
                color: this.currentBrushColor,
                width: this.currentBrushWidth
            });
            
            console.log('Path saved:', this.paths[this.paths.length - 1]);
            
            // Показываем кнопку подтверждения
            const confirmButtons = this.container.querySelectorAll('.confirm-edit-btn');
            confirmButtons.forEach(btn => {
                btn.style.display = 'flex';
            });
            
            this.currentPath = [];
        }
    }

    /**
     * Обрабатывает нажатие кнопки мыши
     * @param {MouseEvent} event - Событие нажатия кнопки мыши
     * @private
     */
    _handleMouseDown(event) {
        // Не начинаем перетаскивание, если активен режим рисования
        if (this.isDrawing || this.isTextMode) return;
        
        // Начинаем перетаскивание только при нажатии левой кнопки мыши
        if (event.button === 0) {
            this.isPanning = true;
            this.lastMouseX = event.clientX;
            this.lastMouseY = event.clientY;
            
            // Сохраняем начальную позицию для проверки перемещения
            this.dragStartX = this.offsetX;
            this.dragStartY = this.offsetY;
            
            this.canvas.style.cursor = 'grabbing';
            
            // Эффект "нажатия" при захвате изображения
            if (window.gsap) {
                gsap.to(this.canvas, { duration: 0.1, scale: this.scale * 0.99 });
            }
        }
    }
    
    /**
     * Обрабатывает перемещение мыши
     * @param {MouseEvent} event - Событие перемещения мыши
     * @private
     */
    _handleMouseMove(event) {
        if (this.isPanning) {
            // Рассчитываем разницу между текущим и предыдущим положением курсора
            const deltaX = event.clientX - this.lastMouseX;
            const deltaY = event.clientY - this.lastMouseY;
            
            // Обновляем смещение изображения
            this.offsetX += deltaX;
            this.offsetY += deltaY;
            
            // Обновляем предыдущее положение курсора
            this.lastMouseX = event.clientX;
            this.lastMouseY = event.clientY;
            
            // Обновляем canvas
            this._updateCanvas();
        }
    }
    
    /**
     * Обрабатывает отпускание кнопки мыши
     * @param {MouseEvent} event - Событие отпускания кнопки мыши
     * @private
     */
    _handleMouseUp(event) {
        if (this.isPanning) {
            this.isPanning = false;
            this.canvas.style.cursor = 'grab';
            
            // Показываем кнопку подтверждения после перемещения изображения
            if (this.dragStartX !== undefined && this.dragStartY !== undefined && 
                (this.dragStartX !== this.offsetX || this.dragStartY !== this.offsetY)) {
                this.isEdited = true;
                const confirmButtons = this.container.querySelectorAll('.confirm-edit-btn');
                confirmButtons.forEach(btn => {
                    btn.style.display = 'flex';
                });
            }
        }
    }
    
    /**
     * Отображает изображение на холсте с учетом текущего масштаба и положения
     * @private
     */
    _updateCanvas() {
        if (!this.ctx || !this.canvas) return;
        
        // Сохраняем трансформацию
        this.ctx.save();
        
        // Очищаем холст
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Применяем масштаб и смещение
        this.ctx.translate(this.offsetX, this.offsetY);
        this.ctx.scale(this.scale, this.scale);
        
        // Отрисовываем основное изображение
        this.ctx.drawImage(this.img, 0, 0, this.img.naturalWidth, this.img.naturalHeight);
        
        // Перерисовываем сохраненные пути
        if (this.paths && this.paths.length > 0) {
            this.paths.forEach(pathData => {
                if (!pathData.path || pathData.path.length < 2) return;
                
                this.ctx.beginPath();
                this.ctx.strokeStyle = pathData.color || this.currentBrushColor;
                this.ctx.lineWidth = pathData.width || this.currentBrushWidth;
                this.ctx.lineCap = 'round';
                this.ctx.lineJoin = 'round';
                
                // Учитываем уже примененную трансформацию
                const firstPoint = pathData.path[0];
                this.ctx.moveTo(firstPoint.x, firstPoint.y);
                
                // Рисуем линии ко всем остальным точкам
                for (let i = 1; i < pathData.path.length; i++) {
                    this.ctx.lineTo(pathData.path[i].x, pathData.path[i].y);
                }
                
                this.ctx.stroke();
                this.ctx.closePath();
            });
        }
        
        // Восстанавливаем трансформацию
        this.ctx.restore();
    }
    
    /**
     * Сбрасывает масштаб и позицию изображения
     * @private
     */
    _resetZoom() {
        // Анимируем сброс масштаба
        if (window.gsap) {
            const targetScale = 1;
            const targetOffsetX = (this.canvas.width - this.img.naturalWidth) / 2;
            const targetOffsetY = (this.canvas.height - this.img.naturalHeight) / 2;
            
            gsap.to(this, {
                scale: targetScale,
                offsetX: targetOffsetX,
                offsetY: targetOffsetY,
                duration: 0.5,
                ease: "power2.out",
                onUpdate: () => this._updateCanvas()
            });
        } else {
            // Версия без анимации
            this.scale = 1;
            this.offsetX = (this.canvas.width - this.img.naturalWidth) / 2;
            this.offsetY = (this.canvas.height - this.img.naturalHeight) / 2;
            this._updateCanvas();
        }
    }
    
    /**
     * Закрывает просмотрщик изображений
     */
    close() {
        // Анимируем закрытие просмотрщика
        if (window.gsap) {
            gsap.to(this.container, {
                opacity: 0,
                duration: 0.3,
                ease: "power1.in",
                onComplete: () => {
                    // Удаляем обработчики событий
                    this._removeEventListeners();
                    
                    // Удаляем контейнер из DOM
                    if (this.container && this.container.parentNode) {
                        this.container.classList.remove('active');
                        this.container.parentNode.removeChild(this.container);
                    }
                }
            });
        } else {
            // Версия без анимации
            // Удаляем обработчики событий
            this._removeEventListeners();
            
            // Удаляем контейнер из DOM
            if (this.container && this.container.parentNode) {
                this.container.classList.remove('active');
                this.container.parentNode.removeChild(this.container);
            }
        }
    }
    
    /**
     * Удаляет все обработчики событий
     * @private
     */
    _removeEventListeners() {
        if (this.canvas) {
            // Чтобы избежать дублирования и утечек памяти,
            // создаем временные ссылки на связанные методы
            if (!this._boundHandleMouseDown) {
                this._boundHandleMouseDown = this._handleMouseDown.bind(this);
                this._boundHandleWheel = this._handleWheel.bind(this);
                this._boundHandleDrawStart = this._handleDrawStart.bind(this);
                this._boundHandleDrawMove = this._handleDrawMove.bind(this);
                this._boundHandleDrawEnd = this._handleDrawEnd.bind(this);
                this._boundHandleMouseMove = this._handleMouseMove.bind(this);
                this._boundHandleMouseUp = this._handleMouseUp.bind(this);
            }
            
            // Удаляем обработчики
            this.canvas.removeEventListener('mousedown', this._boundHandleMouseDown);
            this.canvas.removeEventListener('wheel', this._boundHandleWheel);
            this.canvas.removeEventListener('mousedown', this._boundHandleDrawStart);
            this.canvas.removeEventListener('mousemove', this._boundHandleDrawMove);
            this.canvas.removeEventListener('mouseup', this._boundHandleDrawEnd);
            this.canvas.removeEventListener('mouseout', this._boundHandleDrawEnd);
            
            // Удаляем обработчик текста, если он есть
            if (this.textClickHandler) {
                this.canvas.removeEventListener('click', this.textClickHandler);
            }
        }
        
        document.removeEventListener('mousemove', this._boundHandleMouseMove);
        document.removeEventListener('mouseup', this._boundHandleMouseUp);
        
        // Очищаем ссылки на обработчики
        this.textClickHandler = null;
    }

    /**
     * Открывает просмотрщик изображений с заданным изображением
     * @param {string} src - URL изображения
     * @param {Function} onSave - Функция обратного вызова для сохранения
     * @param {Function} onConfirm - Функция обратного вызова для подтверждения
     */
    open(src, onSave = null, onConfirm = null) {
        console.log('Opening image viewer with source:', src);
        
        // Проверяем, существует ли контейнер
        if (!this.container || !document.body.contains(this.container)) {
            this._createViewerContainer();
        }
        
        // Очищаем предыдущие изображения и создаем canvas
        const containerElement = document.getElementById(this.options.containerId || 'imageViewerContainer');
        
        if (containerElement) {
            // Сбрасываем состояние просмотрщика
            this.isEdited = false;
            this.paths = [];
            this.currentPath = [];
            this.scale = 1;
            this.offsetX = 0;
            this.offsetY = 0;
            this.isDrawing = false;
            this.isTextMode = false;
            
            // Сохраняем callback для подтверждения
            this.onConfirmCallback = onConfirm;
            
            // Показываем контейнер
            containerElement.style.display = 'flex';
            containerElement.style.opacity = '0';
            
            // Загружаем изображение
            const imgElement = new Image();
            imgElement.crossOrigin = "anonymous"; // Для работы с изображениями с других доменов
            imgElement.src = src;
            
            // Обработчик загрузки изображения
            imgElement.onload = () => {
                console.log('Image loaded successfully:', imgElement.naturalWidth, 'x', imgElement.naturalHeight);
                
                // Сохраняем ссылку на изображение
                this.img = imgElement;
                
                // Инициализируем canvas после загрузки изображения
                this._initCanvas();
                
                // Скрываем анимацию загрузки
                this._hideLoadingAnimation();
                
                // Добавляем обработчики событий
                this._initEventListeners();
                
                // Анимируем появление
                if (window.gsap) {
                    gsap.fromTo(containerElement, 
                        {opacity: 0, y: 20}, 
                        {opacity: 1, y: 0, duration: 0.3, ease: "power2.out"}
                    );
                } else {
                    containerElement.style.opacity = '1';
                }
                
                // Анимируем появление изображения
                this._animateImageAppearance();
            };
            
            imgElement.onerror = (error) => {
                console.error('Error loading image:', error);
                this._hideLoadingAnimation();
                this._showErrorMessage('Ошибка загрузки изображения');
            };
            
            // Показываем анимацию загрузки
            this._showLoadingAnimation();
        } else {
            console.error('Image viewer container not found');
        }
    }
    
    /**
     * Инициализирует холст для рисования
     * @private
     */
    _initCanvas() {
        // Удаляем предыдущий canvas, если он существует
        if (this.canvas) {
            this.canvas.remove();
        }
        
        // Создаем новый canvas
        this.canvas = document.createElement('canvas');
        this.canvas.className = 'imageviewer-canvas';
        
        // Устанавливаем размеры canvas равными размерам изображения
        this.canvas.width = this.img.naturalWidth;
        this.canvas.height = this.img.naturalHeight;
        
        // Добавляем canvas в контейнер как первый элемент
        const content = this.container.querySelector('.image-viewer-content');
        content.insertBefore(this.canvas, content.firstChild);
        
        // Получаем контекст для рисования
        this.ctx = this.canvas.getContext('2d');
        
        // Отображаем изображение на canvas
        this.ctx.drawImage(this.img, 0, 0, this.canvas.width, this.canvas.height);
        
        // Сбрасываем пути и масштаб
        this.paths = [];
        this.currentPath = [];
        this.scale = 1;
        this.offsetX = 0;
        this.offsetY = 0;
        
        // Скрываем оригинальное изображение, теперь у нас есть canvas
        this.img.style.display = 'none';
        
        // Делаем canvas перетаскиваемым
        this.canvas.style.cursor = 'grab';
        
        // Стилизуем canvas для правильного отображения
        Object.assign(this.canvas.style, {
            maxWidth: '100%',
            maxHeight: '80vh',
            objectFit: 'contain',
            display: 'block',
            margin: '0 auto'
        });
        
        console.log('Canvas initialized', this.canvas.width, this.canvas.height);
    }
    
    /**
     * Обрабатывает изменение масштаба при прокрутке колесом мыши
     * @param {WheelEvent} event - Событие прокрутки
     * @private
     */
    _handleWheel(event) {
        event.preventDefault();

        // Определяем направление скролла
        const delta = event.deltaY || event.detail || event.wheelDelta;
        
        // Вычисляем новый масштаб
        const scaleChange = delta > 0 ? 0.9 : 1.1;
        const newScale = this.scale * scaleChange;
        
        // Проверяем, чтобы новый масштаб был в допустимых пределах
        if (newScale >= this.minScale && newScale <= this.maxScale) {
            // Запоминаем старый масштаб
            const oldScale = this.scale;
            
            // Позиция курсора относительно канваса
            const rect = this.canvas.getBoundingClientRect();
            const mouseX = event.clientX - rect.left;
            const mouseY = event.clientY - rect.top;
            
            // Позиция курсора относительно изображения
            const imgX = (mouseX - this.offsetX) / oldScale;
            const imgY = (mouseY - this.offsetY) / oldScale;
            
            // Обновляем масштаб
            this.scale = newScale;
            
            // Обновляем смещение, чтобы сохранить позицию курсора
            this.offsetX = mouseX - imgX * this.scale;
            this.offsetY = mouseY - imgY * this.scale;
            
            // Обновляем холст
            this._updateCanvas();
            
            // Показываем кнопку подтверждения после изменения масштаба
            this.isEdited = true;
            const confirmButtons = this.container.querySelectorAll('.confirm-edit-btn');
            confirmButtons.forEach(btn => {
                btn.style.display = 'flex';
            });
        }
    }
    
    /**
     * Увеличивает масштаб изображения
     * @private
     */
    _zoomIn() {
        const zoom = 1.2; // 20% увеличение
        const newScale = this.scale * zoom;
        
        // Ограничиваем масштаб
        if (newScale > 10) return;
        
        // Рассчитываем центр видимой области
        const centerX = this.canvas.width / 2;
        const centerY = this.canvas.height / 2;
        
        // Рассчитываем новые смещения, чтобы сохранить центр
        const offsetX = centerX - (centerX - this.offsetX) * zoom;
        const offsetY = centerY - (centerY - this.offsetY) * zoom;
        
        // Анимируем изменение масштаба
        if (window.gsap) {
            gsap.to(this, {
                scale: newScale,
                offsetX: offsetX,
                offsetY: offsetY,
                duration: 0.3,
                ease: "power2.out",
                onUpdate: () => this._updateCanvas()
            });
        } else {
            // Обновляем масштаб без анимации
            this.scale = newScale;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this._updateCanvas();
        }
    }
    
    /**
     * Уменьшает масштаб изображения
     * @private
     */
    _zoomOut() {
        const zoom = 0.8; // 20% уменьшение
        const newScale = this.scale * zoom;
        
        // Ограничиваем масштаб
        if (newScale < 0.1) return;
        
        // Рассчитываем центр видимой области
        const centerX = this.canvas.width / 2;
        const centerY = this.canvas.height / 2;
        
        // Рассчитываем новые смещения, чтобы сохранить центр
        const offsetX = centerX - (centerX - this.offsetX) * zoom;
        const offsetY = centerY - (centerY - this.offsetY) * zoom;
        
        // Анимируем изменение масштаба
        if (window.gsap) {
            gsap.to(this, {
                scale: newScale,
                offsetX: offsetX,
                offsetY: offsetY,
                duration: 0.3,
                ease: "power2.out",
                onUpdate: () => this._updateCanvas()
            });
        } else {
            // Обновляем масштаб без анимации
            this.scale = newScale;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this._updateCanvas();
        }
    }

    /**
     * Включает режим рисования на изображении
     * @private
     */
    _enableDrawMode() {
        if (!this.canvas) {
            console.error('Canvas not initialized');
            return;
        }
        
        // Отключаем обработчики масштабирования и перемещения
        this.isPanning = false;
        
        // Включаем режим рисования
        this.isDrawing = true;
        this.isTextMode = false;
        this.drawingStarted = false;
        
        // Изменяем курсор на кисть
        this.canvas.style.cursor = 'crosshair';
        
        // Обновляем активную кнопку
        this._updateActiveToolButton('drawMode');
        
        // Отображаем панель инструментов рисования
        const drawingToolbar = this.container.querySelector('.drawing-toolbar');
        if (drawingToolbar) {
            // Очищаем существующее содержимое
            drawingToolbar.innerHTML = '';
            
            // Добавляем элементы для рисования
            drawingToolbar.innerHTML = `
                <div class="color-picker-container">
                    <input type="color" id="colorPicker" value="${this.currentBrushColor}">
                    <label for="colorPicker" class="color-picker-label" title="Выбрать цвет">
                        <i class="ri-palette-line"></i>
                    </label>
                </div>
                <select id="brushSize" class="brush-size-select" title="Размер кисти">
                    <option value="1">1px</option>
                    <option value="3">3px</option>
                    <option value="5" selected>5px</option>
                    <option value="10">10px</option>
                </select>
                <button class="image-viewer-save-btn" id="saveImage" title="Сохранить изменения">
                    <i class="ri-save-line"></i> Сохранить
                </button>
            `;
            
            // Показываем панель
            drawingToolbar.style.display = 'flex';
            
            // Устанавливаем цвет и размер кисти
            const colorPicker = drawingToolbar.querySelector('#colorPicker');
            if (colorPicker) {
                colorPicker.value = this.currentBrushColor;
                colorPicker.addEventListener('change', (e) => {
                    this.currentBrushColor = e.target.value;
                    if (this.ctx) {
                        this.ctx.strokeStyle = this.currentBrushColor;
                        this.ctx.fillStyle = this.currentBrushColor;
                    }
                });
            }
            
            const brushSize = drawingToolbar.querySelector('#brushSize');
            if (brushSize) {
                brushSize.value = this.currentBrushWidth;
                brushSize.addEventListener('change', (e) => {
                    this.currentBrushWidth = parseInt(e.target.value);
                    if (this.ctx) {
                        this.ctx.lineWidth = this.currentBrushWidth;
                    }
                });
            }
            
            const saveBtn = drawingToolbar.querySelector('#saveImage');
            if (saveBtn) {
                saveBtn.addEventListener('click', this._saveEdits.bind(this));
            }
        } else {
            // Если панель не найдена, создаем её
            this._createDrawingToolbar();
        }
        
        // Настраиваем контекст канваса для рисования
        if (this.ctx) {
            this.ctx.strokeStyle = this.currentBrushColor;
            this.ctx.lineWidth = this.currentBrushWidth;
            this.ctx.lineCap = 'round';
            this.ctx.lineJoin = 'round';
        }
        
        // Добавляем канвасу класс active для включения событий мыши
        this.canvas.classList.add('active');
        
        console.log('Drawing mode enabled', {
            color: this.currentBrushColor,
            width: this.currentBrushWidth
        });
    }
    
    /**
     * Создает панель инструментов рисования, если она отсутствует
     * @private
     */
    _createDrawingToolbar() {
        // Проверяем, существует ли уже панель
        let drawingToolbar = this.container.querySelector('.drawing-toolbar');
        if (drawingToolbar) return;
        
        // Создаем панель инструментов
        drawingToolbar = document.createElement('div');
        drawingToolbar.className = 'drawing-toolbar';
        drawingToolbar.style.display = 'flex';
        
        // Добавляем выбор цвета
        drawingToolbar.innerHTML = `
            <div class="color-picker-container">
                <input type="color" id="colorPicker" value="${this.currentBrushColor}">
                <label for="colorPicker" class="color-picker-label" title="Выбрать цвет">
                    <i class="ri-palette-line"></i>
                </label>
            </div>
            <select id="brushSize" class="brush-size-select" title="Размер кисти">
                <option value="1">1px</option>
                <option value="3">3px</option>
                <option value="5" selected>5px</option>
                <option value="10">10px</option>
            </select>
            <button class="image-viewer-save-btn" id="saveImage" title="Сохранить изменения">
                <i class="ri-save-line"></i> Сохранить
            </button>
        `;
        
        // Добавляем панель в контейнер после канваса
        this.container.querySelector('.image-viewer-content').appendChild(drawingToolbar);
        
        // Добавляем обработчики событий
        const colorPicker = drawingToolbar.querySelector('#colorPicker');
        if (colorPicker) {
            colorPicker.addEventListener('change', (e) => {
                this.currentBrushColor = e.target.value;
                if (this.ctx) {
                    this.ctx.strokeStyle = this.currentBrushColor;
                    this.ctx.fillStyle = this.currentBrushColor;
                }
            });
        }
        
        const brushSize = drawingToolbar.querySelector('#brushSize');
        if (brushSize) {
            brushSize.addEventListener('change', (e) => {
                this.currentBrushWidth = parseInt(e.target.value);
                if (this.ctx) {
                    this.ctx.lineWidth = this.currentBrushWidth;
                }
            });
        }
        
        const saveBtn = drawingToolbar.querySelector('#saveImage');
        if (saveBtn) {
            saveBtn.addEventListener('click', this._saveEdits.bind(this));
        }
    }

    /**
     * Включает режим добавления текста на изображение
     * @private
     */
    _enableTextMode() {
        if (!this.canvas) return;
        
        // Отключаем другие режимы
        this.isPanning = false;
        this.isDrawing = false;
        this.isTextMode = true;
        
        // Удаляем все текстовые слои перед переключением режима
        const existingLayers = this.container.querySelectorAll('.text-layer');
        existingLayers.forEach(layer => layer.remove());
        
        // Изменяем курсор на текстовый
        this.canvas.style.cursor = 'text';
        
        // Обновляем активную кнопку
        this._updateActiveToolButton('textMode');
        
        // Отображаем панель инструментов для текста
        const drawingToolbar = this.container.querySelector('.drawing-toolbar');
        if (drawingToolbar) {
            // Очищаем существующее содержимое
            drawingToolbar.innerHTML = '';
            
            // Добавляем элементы для работы с текстом
            const textControls = document.createElement('div');
            textControls.className = 'text-controls';
            textControls.innerHTML = `
                <input type="text" id="textInput" placeholder="Введите текст" class="text-input">
                <select id="textSize" class="text-size-select">
                    <option value="12">12px</option>
                    <option value="16" selected>16px</option>
                    <option value="24">24px</option>
                    <option value="32">32px</option>
                    <option value="48">48px</option>
                </select>
                <select id="textFont" class="text-font-select">
                    <option value="Arial, sans-serif">Arial</option>
                    <option value="'Times New Roman', serif">Times New Roman</option>
                    <option value="'Courier New', monospace">Courier New</option>
                    <option value="'Impact', sans-serif">Impact</option>
                </select>
                <button id="addTextBtn" class="add-text-btn">Добавить текст</button>
            `;
            
            // Добавляем контейнер выбора цвета и кнопку сохранения
            drawingToolbar.innerHTML += `
                <div class="color-picker-container">
                    <input type="color" id="colorPicker" value="${this.currentBrushColor}">
                    <label for="colorPicker" class="color-picker-label" title="Выбрать цвет">
                        <i class="ri-palette-line"></i>
                    </label>
                </div>
                <button class="image-viewer-save-btn" id="saveImage" title="Сохранить изменения">
                    <i class="ri-save-line"></i> Сохранить
                </button>
            `;
            
            // Сначала добавляем элементы текстового контроля
            drawingToolbar.prepend(textControls);
            
            // Показываем панель инструментов
            drawingToolbar.style.display = 'flex';
            
            // Добавляем обработчики событий
            const colorPicker = drawingToolbar.querySelector('#colorPicker');
            if (colorPicker) {
                colorPicker.addEventListener('change', (e) => {
                    this.currentBrushColor = e.target.value;
                    if (this.ctx) {
                        this.ctx.fillStyle = this.currentBrushColor;
                    }
                });
            }
            
            const saveBtn = drawingToolbar.querySelector('#saveImage');
            if (saveBtn) {
                saveBtn.addEventListener('click', this._saveEdits.bind(this));
            }
            
            // Добавляем обработчик для кнопки добавления текста
            const addTextBtn = drawingToolbar.querySelector('#addTextBtn');
            if (addTextBtn) {
                addTextBtn.addEventListener('click', () => this._addDraggableText());
            }
            
            // Добавляем обработчик для ввода текста по Enter
            const textInput = drawingToolbar.querySelector('#textInput');
            if (textInput) {
                textInput.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        this._addDraggableText();
                    }
                });
                
                // Фокусируемся на поле ввода
                setTimeout(() => textInput.focus(), 100);
            }
        }
    }

    /**
     * Добавляет перемещаемый текст на изображение
     * @private
     */
    _addDraggableText() {
        const textInput = this.container.querySelector('#textInput');
        const textSize = this.container.querySelector('#textSize');
        const textFont = this.container.querySelector('#textFont');
        const colorPicker = this.container.querySelector('#colorPicker');
        
        if (!textInput || !this.ctx) return;
        
        const text = textInput.value.trim();
        if (!text) return;
        
        // Создаем слой для текста
        const textLayer = document.createElement('div');
        textLayer.className = 'text-layer';
        textLayer.textContent = text;
        
        // Стилизуем текстовый слой
        const fontSize = textSize ? textSize.value : 16;
        const fontFamily = textFont ? textFont.value : 'Arial, sans-serif';
        const textColor = colorPicker ? colorPicker.value : this.currentBrushColor;
        
        Object.assign(textLayer.style, {
            position: 'absolute',
            left: '50%',
            top: '50%',
            transform: 'translate(-50%, -50%)',
            color: textColor,
            fontSize: `${fontSize}px`,
            fontFamily: fontFamily,
            cursor: 'move',
            userSelect: 'none',
            padding: '5px',
            zIndex: '1000',
            textShadow: '0px 0px 2px rgba(0,0,0,0.5)',
            backgroundColor: 'rgba(255, 255, 255, 0.1)' // Легкий фон для видимости при перетаскивании
        });
        
        // Добавляем кнопку для завершения редактирования текста
        const doneButton = document.createElement('button');
        doneButton.className = 'text-layer-done';
        doneButton.innerHTML = '<i class="ri-check-line"></i>';
        Object.assign(doneButton.style, {
            position: 'absolute',
            top: '-20px',
            right: '-20px',
            width: '24px',
            height: '24px',
            borderRadius: '50%',
            background: 'rgba(40, 167, 69, 0.85)',
            color: 'white',
            border: 'none',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            zIndex: '1001'
        });
        
        // Кнопка для удаления текста
        const deleteButton = document.createElement('button');
        deleteButton.className = 'text-layer-delete';
        deleteButton.innerHTML = '<i class="ri-delete-bin-line"></i>';
        Object.assign(deleteButton.style, {
            position: 'absolute',
            top: '-20px',
            left: '-20px',
            width: '24px',
            height: '24px',
            borderRadius: '50%',
            background: 'rgba(220, 53, 69, 0.85)',
            color: 'white',
            border: 'none',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            zIndex: '1001'
        });
        
        textLayer.appendChild(doneButton);
        textLayer.appendChild(deleteButton);
        
        // Добавляем слой в контейнер
        const containerContent = this.container.querySelector('.image-viewer-content');
        containerContent.appendChild(textLayer);
        
        // Делаем текст перетаскиваемым
        let isDragging = false;
        let offsetX = 0;
        let offsetY = 0;
        
        const handleMouseDown = (e) => {
            if (e.target === doneButton || e.target.parentElement === doneButton ||
                e.target === deleteButton || e.target.parentElement === deleteButton) {
                return;
            }
            
            isDragging = true;
            
            // Получаем текущие координаты слоя
            const rect = textLayer.getBoundingClientRect();
            
            // Вычисляем смещение курсора относительно верхнего левого угла слоя
            offsetX = e.clientX - rect.left;
            offsetY = e.clientY - rect.top;
            
            // Убираем transform для точного позиционирования только если он присутствует
            if (textLayer.style.transform === 'translate(-50%, -50%)') {
                // Фиксируем текущее положение перед перетаскиванием
                const containerRect = containerContent.getBoundingClientRect();
                
                // Вычисляем абсолютные координаты
                const absoluteX = rect.left - containerRect.left + (rect.width / 2);
                const absoluteY = rect.top - containerRect.top + (rect.height / 2);
                
                textLayer.style.left = `${absoluteX}px`;
                textLayer.style.top = `${absoluteY}px`;
                textLayer.style.transform = 'none';
            }
            
            // Применяем стиль курсора
            textLayer.style.cursor = 'grabbing';
            
            // Увеличиваем z-index во время перетаскивания
            textLayer.style.zIndex = '1002';
            
            // Предотвращаем дефолтное поведение браузера
            e.preventDefault();
            e.stopPropagation();
        };
        
        const handleMouseMove = (e) => {
            if (!isDragging) return;
            
            // Получаем контейнер и его границы
            const containerRect = containerContent.getBoundingClientRect();
            
            // Вычисляем новую позицию с ограничением внутри контейнера
            const x = Math.max(0, Math.min(e.clientX - containerRect.left - offsetX, containerRect.width - textLayer.offsetWidth));
            const y = Math.max(0, Math.min(e.clientY - containerRect.top - offsetY, containerRect.height - textLayer.offsetHeight));
            
            // Обновляем позицию текстового слоя
            textLayer.style.left = `${x}px`;
            textLayer.style.top = `${y}px`;
            
            // Предотвращаем дефолтное поведение браузера
            e.preventDefault();
            e.stopPropagation();
        };
        
        const handleMouseUp = () => {
            if (isDragging) {
                isDragging = false;
                textLayer.style.cursor = 'move';
                // Сбрасываем z-index после перетаскивания
                textLayer.style.zIndex = '1000';
            }
        };
        
        // Привязываем обработчики к текстовому слою и документу
        textLayer.addEventListener('mousedown', handleMouseDown);
        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);
        
        // Обработчик для кнопки "готово"
        doneButton.addEventListener('click', () => {
            // Получаем позицию текста относительно canvas
            const canvasRect = this.canvas.getBoundingClientRect();
            const containerRect = this.container.querySelector('.image-viewer-content').getBoundingClientRect();
            const textRect = textLayer.getBoundingClientRect();
            
            // Корректируем координаты с учетом масштаба
            const scaleX = this.canvas.width / canvasRect.width;
            const scaleY = this.canvas.height / canvasRect.height;
            
            // Вычисляем позицию текста относительно canvas
            const canvasX = ((textRect.left - containerRect.left) + (textRect.width / 2)) * scaleX;
            const canvasY = ((textRect.top - containerRect.top) + (textRect.height / 2)) * scaleY;
            
            // Рисуем текст на canvas
            this.ctx.font = `${fontSize}px ${fontFamily}`;
            this.ctx.fillStyle = textColor;
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle';
            this.ctx.fillText(text, canvasX, canvasY);
            
            // Удаляем обработчики событий
            textLayer.removeEventListener('mousedown', handleMouseDown);
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
            
            // Удаляем текстовый слой
            textLayer.remove();
            
            // Показываем кнопку подтверждения
            const confirmButtons = this.container.querySelectorAll('.confirm-edit-btn');
            confirmButtons.forEach(btn => {
                btn.style.display = 'flex';
            });
            
            // Отмечаем, что изображение было отредактировано
            this.isEdited = true;
            
            // Очищаем поле ввода
            textInput.value = '';
        });
        
        // Обработчик для кнопки удаления
        deleteButton.addEventListener('click', () => {
            // Удаляем обработчики событий перед удалением элемента
            textLayer.removeEventListener('mousedown', handleMouseDown);
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
            
            textLayer.remove();
        });
    }

    /**
     * Выделяет активную кнопку инструмента
     * @param {string} activeToolId - ID активной кнопки
     * @private
     */
    _updateActiveToolButton(activeToolId) {
        // Убираем выделение со всех кнопок
        const toolButtons = this.container.querySelectorAll('.image-viewer-tool');
        toolButtons.forEach(btn => btn.classList.remove('active'));
        
        // Выделяем активную кнопку
        const activeButton = this.container.querySelector(`#${activeToolId}`);
        if (activeButton) {
            activeButton.classList.add('active');
        }
    }

    /**
     * Сохраняет изменения в изображении
     * @private
     */
    _saveEdits() {
        if (!this.canvas) return;
        
        // Получаем данные изображения в формате base64
        const dataUrl = this.canvas.toDataURL('image/png');
        
        // Обновляем исходное изображение
        this.img.src = dataUrl;
        this.currentImage = dataUrl;
        
        // Скрываем панель инструментов рисования
        const drawingToolbar = this.container.querySelector('.drawing-toolbar');
        if (drawingToolbar) {
            drawingToolbar.style.display = 'none';
        }
        
        // Отключаем режим рисования
        this._disableDrawMode();
        
        // Сбрасываем состояние инструментов
        this.isDrawing = false;
        this.isTextMode = false;
        this.isCropMode = false;
        
        // Отмечаем, что изображение было отредактировано
        this.isEdited = true;
        
        // Показываем кнопку подтверждения
        const confirmButtons = this.container.querySelectorAll('.confirm-edit-btn');
        confirmButtons.forEach(btn => {
            btn.style.display = 'flex';
        });
        
        // Вызываем callback, если он был передан
        if (typeof this.onSaveCallback === 'function') {
            this.onSaveCallback(dataUrl);
        }
        
        // Показываем уведомление об успешном сохранении
        this._showSuccessMessage('Изображение сохранено');
    }

    /**
     * Показывает уведомление об успешном действии
     * @param {string} message - Текст сообщения
     * @private
     */
    _showSuccessMessage(message) {
        const successContainer = document.createElement('div');
        successContainer.className = 'image-viewer-success';
        successContainer.textContent = message;
        
        // Стилизуем контейнер для уведомления
        Object.assign(successContainer.style, {
            position: 'absolute',
            top: '20px',
            left: '50%',
            transform: 'translateX(-50%)',
            backgroundColor: 'rgba(40, 167, 69, 0.85)',
            color: 'white',
            padding: '10px 20px',
            borderRadius: '4px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.2)',
            zIndex: 1005,
            opacity: 0
        });
        
        this.container.appendChild(successContainer);
        
        // Анимируем появление и исчезновение
        if (window.gsap) {
            gsap.timeline()
                .to(successContainer, {opacity: 1, duration: 0.3})
                .to(successContainer, {opacity: 0, duration: 0.3, delay: 1.5, onComplete: () => {
                    successContainer.remove();
                }});
        } else {
            // Запасной вариант без анимации
            successContainer.style.opacity = '1';
            setTimeout(() => {
                successContainer.style.opacity = '0';
                setTimeout(() => successContainer.remove(), 300);
            }, 1500);
        }
    }

    /**
     * Подтверждает редактирование и вызывает callback с отредактированным изображением
     * @private
     */
    _confirmEdit() {
        if (!this.canvas) return;
        
        try {
            // Если изображение не было изменено, создаем его копию прямо сейчас
            if (!this.isEdited) {
                // Получаем данные изображения в формате base64
                const dataUrl = this.canvas.toDataURL('image/png');
                this.currentImage = dataUrl;
                this.isEdited = true;
            } else {
                // Сохраняем отредактированное изображение
                this.currentImage = this.canvas.toDataURL('image/png');
            }
            
            console.log('Confirming edit, image data length:', this.currentImage.length);
            
            try {
                // Пытаемся прикрепить изображение к сообщению
                if (typeof this.onConfirmCallback === 'function') {
                    // Вызываем callback с отредактированным изображением
                    this.onConfirmCallback(this.currentImage);
                } else {
                    // Если callback не передан, прикрепляем изображение стандартным способом
                    this._directAttachImage(this.currentImage);
                }
                
                // Показываем уведомление об успешном подтверждении
                this._showSuccessMessage('Изображение прикреплено к сообщению');
                
                // Анимируем успешное подтверждение
                const confirmBtn = this.container.querySelector('.confirm-edit-btn');
                if (confirmBtn && window.gsap) {
                    gsap.timeline()
                        .to(confirmBtn, {scale: 1.2, duration: 0.2})
                        .to(confirmBtn, {scale: 1, duration: 0.2})
                        .to(this.container, {opacity: 0, duration: 0.3, delay: 0.2, onComplete: () => {
                            this.close();
                        }});
                } else {
                    // Закрываем просмотрщик
                    this.close();
                }
            } catch (e) {
                console.error('Ошибка при прикреплении изображения:', e);
                this._showErrorMessage('Ошибка при прикреплении изображения');
            }
        } catch (e) {
            console.error('Ошибка при сохранении изображения:', e);
            this._showErrorMessage('Ошибка при сохранении изображения');
        }
    }

    /**
     * Непосредственно прикрепляет изображение к сообщению, минуя MessengerAPI
     * @param {string} imageDataUrl - Данные изображения в формате base64
     * @private
     */
    _directAttachImage(imageDataUrl) {
        // Находим контейнер для предпросмотра вложений
        const previewContainer = document.getElementById('attachment-preview');
        
        if (!previewContainer) {
            console.error('Контейнер предпросмотра не найден');
            this._showErrorMessage('Ошибка при прикреплении изображения');
            return;
        }
        
        // Очищаем контейнер и устанавливаем класс
        previewContainer.innerHTML = '';
        previewContainer.classList.add('active');
        
        // Создаем обертку для предпросмотра
        const previewWrapper = document.createElement('div');
        previewWrapper.className = 'attachment-preview-wrapper';
        
        // Создаем предпросмотр изображения
        const imgContainer = document.createElement('div');
        imgContainer.className = 'attachment-image-container';
        
        // Создаем изображение
        const img = document.createElement('img');
        img.className = 'attachment-preview-image';
        
        // Отслеживаем загрузку изображения
        img.onload = () => {
            console.log('Предпросмотр изображения загружен успешно');
            img.classList.add('loaded');
        };
        
        img.onerror = (error) => {
            console.error('Ошибка загрузки предпросмотра:', error);
        };
        
        // Устанавливаем src после добавления обработчиков, чтобы они успели сработать
        img.src = imageDataUrl;
        
        // Добавляем изображение в контейнер
        imgContainer.appendChild(img);
        previewWrapper.appendChild(imgContainer);
        
        // Добавляем информацию о файле
        const fileInfo = document.createElement('div');
        fileInfo.className = 'attachment-file-info';
        fileInfo.innerHTML = `
            <div class="attachment-file-name">Отредактированное изображение</div>
            <div class="attachment-file-size">Изображение PNG</div>
        `;
        previewWrapper.appendChild(fileInfo);
        
        // Добавляем кнопку удаления
        const removeButton = document.createElement('button');
        removeButton.className = 'remove-attachment-btn';
        removeButton.setAttribute('title', 'Удалить вложение');
        removeButton.innerHTML = '<i class="ri-close-line"></i>';
        removeButton.addEventListener('click', (e) => {
            e.stopPropagation();
            if (typeof MessengerAttachment !== 'undefined' && 
                typeof MessengerAttachment.clearAttachments === 'function') {
                MessengerAttachment.clearAttachments();
            } else {
                previewContainer.innerHTML = '';
                previewContainer.classList.remove('active');
            }
        });
        previewWrapper.appendChild(removeButton);
        
        // Добавляем обертку в контейнер
        previewContainer.appendChild(previewWrapper);
        
        // Сохраняем ссылку на изображение для отправки
        previewContainer.imageData = imageDataUrl;
        
        // Создаем Blob и File из dataURL для совместимости с кодом отправки
        try {
            const byteString = atob(imageDataUrl.split(',')[1]);
            const mimeType = imageDataUrl.split(',')[0].split(':')[1].split(';')[0];
            const arrayBuffer = new ArrayBuffer(byteString.length);
            const uint8Array = new Uint8Array(arrayBuffer);
            
            for (let i = 0; i < byteString.length; i++) {
                uint8Array[i] = byteString.charCodeAt(i);
            }
            
            const blob = new Blob([arrayBuffer], { type: mimeType });
            const file = new File([blob], 'edited_image.png', { type: 'image/png' });
            
            // Сохраняем объект File в контейнере
            previewContainer.file = file;
            
            console.log('Объект File создан успешно из изображения размером', file.size, 'байт');
            
            // Активируем элементы UI для вложения
            const attachmentButton = document.getElementById('attachment-button');
            if (attachmentButton) {
                attachmentButton.classList.add('attachment-button-active');
            }
            
            const messageInputContainer = document.querySelector('.message-input-container');
            if (messageInputContainer) {
                messageInputContainer.classList.add('has-attachment');
            }
            
            console.log('Изображение прикреплено напрямую');
        } catch (error) {
            console.error('Ошибка создания файла из изображения:', error);
        }
    }

    /**
     * Отключает режим рисования
     * @private
     */
    _disableDrawMode() {
        this.isDrawing = false;
        this.drawingStarted = false;
        
        // Изменяем стиль курсора
        if (this.canvas) {
            this.canvas.style.cursor = 'grab';
            this.canvas.classList.remove('active');
        }
        
        // Скрываем панель инструментов рисования
        const drawingToolbar = this.container.querySelector('.drawing-toolbar');
        if (drawingToolbar) {
            drawingToolbar.style.display = 'none';
        }
        
        // Убираем класс активного режима рисования
        this.container.classList.remove('draw-mode-active');
    }
}

// Создаем и экспортируем глобальный экземпляр просмотрщика
window.imageViewer = new ImageViewer();

/**
 * Инициализирует обработчики для открытия просмотрщика при клике на изображения
 * Необходимо вызвать эту функцию после загрузки DOM
 */
function initImageViewerHandlers() {
    console.log('Initializing image viewer handlers');
    // Находим все изображения в чате
    const chatImages = document.querySelectorAll('.chat-message-image img, .image-attachment img, .message-image');
    
    // Добавляем обработчик клика для каждого изображения
    chatImages.forEach(img => {
        // Удаляем предыдущие обработчики, чтобы избежать дублирования
        img.removeEventListener('click', handleImageClick);
        // Добавляем новый обработчик
        img.addEventListener('click', handleImageClick);
    });
    
    console.log(`Found ${chatImages.length} images to attach handlers to`);
}

/**
 * Обработчик клика по изображению
 * @param {Event} e - Событие клика
 */
function handleImageClick(e) {
    e.preventDefault();
    e.stopPropagation();
    
    // Получаем URL изображения
    const imageSrc = e.target.src || e.target.dataset.src;
    if (!imageSrc) {
        console.error('Источник изображения не найден');
        return;
    }
    
    console.log('Открываем просмотрщик изображений для:', imageSrc);
    
    // Открываем просмотрщик
    window.imageViewer.open(imageSrc, null, (editedImageDataUrl) => {
        // Обработчик для подтверждения редактирования
        console.log('Изображение отредактировано, прикрепляем к сообщению');
        
        // Прямое прикрепление изображения к сообщению
        const previewContainer = document.getElementById('attachment-preview');
        if (!previewContainer) {
            console.error('Контейнер предпросмотра не найден');
            showAttachmentNotification('Ошибка при прикреплении изображения', 'error');
            return;
        }
        
        // Очищаем контейнер и устанавливаем класс
        previewContainer.innerHTML = '';
        previewContainer.classList.add('active');
        
        // Создаем обертку для предпросмотра
        const previewWrapper = document.createElement('div');
        previewWrapper.className = 'attachment-preview-wrapper';
        
        // Создаем предпросмотр изображения
        const imgContainer = document.createElement('div');
        imgContainer.className = 'attachment-image-container';
        
        // Создаем изображение
        const img = document.createElement('img');
        img.className = 'attachment-preview-image';
        
        // Отслеживаем загрузку изображения
        img.onload = () => {
            console.log('Предпросмотр изображения загружен успешно');
            img.classList.add('loaded');
        };
        
        img.onerror = (error) => {
            console.error('Ошибка загрузки предпросмотра:', error);
        };
        
        // Устанавливаем src после добавления обработчиков
        img.src = editedImageDataUrl;
        
        // Добавляем изображение в контейнер
        imgContainer.appendChild(img);
        previewWrapper.appendChild(imgContainer);
        
        // Добавляем информацию о файле
        const fileInfo = document.createElement('div');
        fileInfo.className = 'attachment-file-info';
        fileInfo.innerHTML = `
            <div class="attachment-file-name">Отредактированное изображение</div>
            <div class="attachment-file-size">Изображение PNG</div>
        `;
        previewWrapper.appendChild(fileInfo);
        
        // Добавляем кнопку удаления
        const removeButton = document.createElement('button');
        removeButton.className = 'remove-attachment-btn';
        removeButton.setAttribute('title', 'Удалить вложение');
        removeButton.innerHTML = '<i class="ri-close-line"></i>';
        removeButton.addEventListener('click', (e) => {
            e.stopPropagation();
            if (typeof MessengerAttachment !== 'undefined' && 
                typeof MessengerAttachment.clearAttachments === 'function') {
                MessengerAttachment.clearAttachments();
            } else {
                previewContainer.innerHTML = '';
                previewContainer.classList.remove('active');
            }
        });
        previewWrapper.appendChild(removeButton);
        
        // Добавляем обертку в контейнер
        previewContainer.appendChild(previewWrapper);
        
        // Сохраняем ссылку на изображение для отправки
        previewContainer.imageData = editedImageDataUrl;
        
        // Создаем Blob и File из dataURL для совместимости с кодом отправки
        try {
            const byteString = atob(editedImageDataUrl.split(',')[1]);
            const mimeType = editedImageDataUrl.split(',')[0].split(':')[1].split(';')[0];
            const arrayBuffer = new ArrayBuffer(byteString.length);
            const uint8Array = new Uint8Array(arrayBuffer);
            
            for (let i = 0; i < byteString.length; i++) {
                uint8Array[i] = byteString.charCodeAt(i);
            }
            
            const blob = new Blob([arrayBuffer], { type: mimeType });
            const file = new File([blob], 'edited_image.png', { type: 'image/png' });
            
            // Сохраняем объект File в контейнере
            previewContainer.file = file;
            
            console.log('Объект File создан успешно из изображения размером', file.size, 'байт');
            
            // Активируем элементы UI для вложения
            const attachmentButton = document.getElementById('attachment-button');
            if (attachmentButton) {
                attachmentButton.classList.add('attachment-button-active');
            }
            
            const messageInputContainer = document.querySelector('.message-input-container');
            if (messageInputContainer) {
                messageInputContainer.classList.add('has-attachment');
            }
            
            // Обновляем плейсхолдер в текстовом поле
            const messageTextArea = document.getElementById('messageTextArea');
            if (messageTextArea) {
                messageTextArea.placeholder = 'Добавьте подпись к изображению...';
            }
            
            // Показываем уведомление
            showAttachmentNotification('Изображение готово к отправке');
        } catch (error) {
            console.error('Ошибка создания файла из изображения:', error);
            showAttachmentNotification('Ошибка при обработке изображения', 'error');
        }
    });
}

/**
 * Показывает уведомление о прикреплении файла
 * @param {string} message - Текст уведомления
 */
function showAttachmentNotification(message) {
    // Создаем элемент уведомления
    const notification = document.createElement('div');
    notification.className = 'attachment-notification';
    notification.textContent = message;
    
    // Стилизуем уведомление
    Object.assign(notification.style, {
        position: 'fixed',
        bottom: '20px',
        right: '20px',
        backgroundColor: 'rgba(25, 135, 84, 0.9)',
        color: 'white',
        padding: '10px 15px',
        borderRadius: '4px',
        boxShadow: '0 2px 10px rgba(0,0,0,0.2)',
        zIndex: 1100,
        opacity: 0,
        transform: 'translateY(20px)',
        transition: 'opacity 0.3s, transform 0.3s'
    });
    
    // Добавляем уведомление в DOM
    document.body.appendChild(notification);
    
    // Анимируем появление
    setTimeout(() => {
        notification.style.opacity = '1';
        notification.style.transform = 'translateY(0)';
    }, 10);
    
    // Скрываем через 3 секунды
    setTimeout(() => {
        notification.style.opacity = '0';
        notification.style.transform = 'translateY(20px)';
        
        // Удаляем из DOM после анимации
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

// Инициализируем обработчики после загрузки DOM
document.addEventListener('DOMContentLoaded', initImageViewerHandlers);

// Обновляем обработчики при изменении содержимого чата
document.addEventListener('chat-updated', initImageViewerHandlers);

/**
 * Вспомогательная функция для добавления изображения к сообщению
 * в случае, если не удалось использовать API
 * @param {string} imageDataUrl - Данные изображения в формате base64
 */
function appendImageToMessage(imageDataUrl) {
    console.log('Using fallback method to append image to message');
    try {
        // Проверяем, что данные изображения корректны
        if (!imageDataUrl || !imageDataUrl.startsWith('data:image')) {
            console.error('Invalid image data URL format');
            showAttachmentNotification('Некорректный формат изображения', 'error');
            return;
        }
        
        // Находим контейнер вложений сообщения
        const previewContainer = document.getElementById('attachment-preview');
        if (!previewContainer) {
            console.error('Attachment preview container not found');
            return;
        }
        
        // Очищаем контейнер
        previewContainer.innerHTML = '';
        previewContainer.classList.add('active');
        
        // Создаем предпросмотр изображения
        const previewWrapper = document.createElement('div');
        previewWrapper.className = 'attachment-preview-wrapper';
        
        // Создаем изображение
        const img = new Image();
        img.onload = () => {
            console.log('Preview image loaded successfully:', img.width, 'x', img.height);
            img.classList.add('loaded');
        };
        img.onerror = (err) => {
            console.error('Error loading preview image:', err);
        };
        img.src = imageDataUrl;
        img.className = 'attachment-preview-image';
        
        // Создаем контейнер для изображения
        const imgContainer = document.createElement('div');
        imgContainer.className = 'attachment-image-container';
        imgContainer.appendChild(img);
        
        // Добавляем информацию о файле
        const fileInfo = document.createElement('div');
        fileInfo.className = 'attachment-file-info';
        fileInfo.innerHTML = `
            <div class="attachment-file-name">Отредактированное изображение</div>
            <div class="attachment-file-size">Изображение PNG</div>
        `;
        
        // Добавляем кнопку удаления
        const removeButton = document.createElement('button');
        removeButton.className = 'remove-attachment-btn';
        removeButton.setAttribute('title', 'Удалить вложение');
        removeButton.innerHTML = '<i class="ri-close-line"></i>';
        removeButton.addEventListener('click', (e) => {
            e.stopPropagation();
            if (typeof MessengerAttachment !== 'undefined' && typeof MessengerAttachment.clearAttachments === 'function') {
                MessengerAttachment.clearAttachments();
            } else {
                previewContainer.innerHTML = '';
                previewContainer.classList.remove('active');
            }
        });
        
        // Собираем все элементы
        previewWrapper.appendChild(imgContainer);
        previewWrapper.appendChild(fileInfo);
        previewWrapper.appendChild(removeButton);
        previewContainer.appendChild(previewWrapper);
        
        // Создаем объект File из Data URL для совместимости с существующим кодом
        try {
            const byteString = atob(imageDataUrl.split(',')[1]);
            const mimeString = imageDataUrl.split(',')[0].split(':')[1].split(';')[0];
            const ab = new ArrayBuffer(byteString.length);
            const ia = new Uint8Array(ab);
            for (let i = 0; i < byteString.length; i++) {
                ia[i] = byteString.charCodeAt(i);
            }
            const blob = new Blob([ab], {type: mimeString});
            const file = new File([blob], 'edited_image.png', {type: 'image/png'});
            
            // Сохраняем объект File в контейнере
            previewContainer.file = file;
        } catch (error) {
            console.error('Error creating File object from Data URL:', error);
        }
        
        // Сохраняем ссылку на изображение в контейнере
        previewContainer.imageData = imageDataUrl;
        
        // Обновляем интерфейс сообщения
        if (typeof updateAttachmentsUI === 'function') {
            updateAttachmentsUI();
        }
        
        // Активируем кнопку прикрепления
        const attachmentButton = document.getElementById('attachment-button');
        if (attachmentButton) {
            attachmentButton.classList.add('attachment-button-active');
        }
        
        // Добавляем класс для контейнера ввода
        const messageInputContainer = document.querySelector('.message-input-container');
        if (messageInputContainer) {
            messageInputContainer.classList.add('has-attachment');
        }
        
        // Обновляем плейсхолдер в текстовом поле
        const messageTextArea = document.getElementById('messageTextArea');
        if (messageTextArea) {
            const originalPlaceholder = messageTextArea.getAttribute('data-original-placeholder') || 
                                       messageTextArea.placeholder;
            
            // Сохраняем оригинальный placeholder, если он еще не сохранен
            if (!messageTextArea.getAttribute('data-original-placeholder')) {
                messageTextArea.setAttribute('data-original-placeholder', originalPlaceholder);
            }
            
            messageTextArea.placeholder = 'Добавьте подпись к изображению...';
        }
        
        console.log('Image appended to message successfully');
        
        // Показываем уведомление
        showAttachmentNotification('Изображение готово к отправке');
    } catch (error) {
        console.error('Error appending image to message:', error);
        showAttachmentNotification('Ошибка при прикреплении изображения', 'error');
    }
}

/**
 * Инициализирует просмотрщик изображений в DOM
 * @param {HTMLElement} container - Контейнер для просмотрщика
 */
function initializeImageViewer(container = document.body) {
    if (!window.imageViewer) {
        console.log('Creating new ImageViewer instance');
        window.imageViewer = new ImageViewer(container, null, {
            containerId: 'imageViewerContainer',
            brushColor: '#ff0000',
            brushWidth: 5
        });
    }
    
    // Очищаем предыдущие обработчики и устанавливаем новые
    initImageViewerHandlers();
    
    console.log('ImageViewer initialized');
}

// Инициализируем обработчики после загрузки DOM
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing ImageViewer');
    initializeImageViewer();
});

// Обновляем обработчики при изменении содержимого чата
document.addEventListener('chat-updated', () => {
    console.log('Chat updated, reinitializing ImageViewer handlers');
    initImageViewerHandlers();
}); 