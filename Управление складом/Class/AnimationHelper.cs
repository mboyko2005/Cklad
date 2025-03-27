using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Вспомогательный класс для работы с анимациями
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// Создает и запускает анимацию появления элемента
        /// </summary>
        /// <param name="element">Элемент для анимации</param>
        /// <param name="duration">Продолжительность анимации в миллисекундах</param>
        public static void FadeIn(FrameworkElement element, double duration = 300)
        {
            if (element == null)
                return;

            // Сохраняем исходную прозрачность
            double originalOpacity = element.Opacity;
            
            // Устанавливаем начальную прозрачность
            element.Opacity = 0;
            
            // Создаем анимацию прозрачности
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = originalOpacity,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Запускаем анимацию
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        
        /// <summary>
        /// Создает и запускает анимацию исчезновения элемента
        /// </summary>
        /// <param name="element">Элемент для анимации</param>
        /// <param name="duration">Продолжительность анимации в миллисекундах</param>
        /// <param name="removeFromParent">Удалить элемент из родительского контейнера после анимации</param>
        public static void FadeOut(FrameworkElement element, double duration = 300, bool removeFromParent = false)
        {
            if (element == null)
                return;
            
            // Создаем анимацию прозрачности
            DoubleAnimation animation = new DoubleAnimation
            {
                From = element.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Если нужно удалить элемент после завершения анимации
            if (removeFromParent)
            {
                animation.Completed += (s, e) =>
                {
                    if (element.Parent is Panel panel)
                    {
                        panel.Children.Remove(element);
                    }
                };
            }
            
            // Запускаем анимацию
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        
        /// <summary>
        /// Создает и запускает анимацию сдвига элемента
        /// </summary>
        /// <param name="element">Элемент для анимации</param>
        /// <param name="fromX">Начальная позиция X</param>
        /// <param name="toX">Конечная позиция X</param>
        /// <param name="fromY">Начальная позиция Y</param>
        /// <param name="toY">Конечная позиция Y</param>
        /// <param name="duration">Продолжительность анимации в миллисекундах</param>
        public static void SlideAnimation(FrameworkElement element, double fromX, double toX, double fromY, double toY, double duration = 300)
        {
            if (element == null)
                return;
            
            // Создаем трансформацию сдвига
            TranslateTransform transform = new TranslateTransform(fromX, fromY);
            element.RenderTransform = transform;
            
            // Анимация по X
            DoubleAnimation animationX = new DoubleAnimation
            {
                From = fromX,
                To = toX,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Анимация по Y
            DoubleAnimation animationY = new DoubleAnimation
            {
                From = fromY,
                To = toY,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Запускаем анимации
            transform.BeginAnimation(TranslateTransform.XProperty, animationX);
            transform.BeginAnimation(TranslateTransform.YProperty, animationY);
        }
        
        /// <summary>
        /// Создает и запускает анимацию появления сообщения
        /// </summary>
        /// <param name="element">Элемент сообщения</param>
        /// <param name="isOutgoing">Является ли сообщение исходящим</param>
        public static void MessageAppearAnimation(FrameworkElement element, bool isOutgoing)
        {
            if (element == null)
                return;
            
            // Для исходящих сообщений анимируем справа налево
            // Для входящих - слева направо
            double fromX = isOutgoing ? 30 : -30;
            
            // Создаем анимацию сдвига с пропаданием прозрачности
            SlideAnimation(element, fromX, 0, 0, 0, 300);
            FadeIn(element, 250);
        }
        
        /// <summary>
        /// Создает и запускает анимацию удаления сообщения
        /// </summary>
        /// <param name="element">Элемент сообщения</param>
        /// <param name="isOutgoing">Является ли сообщение исходящим</param>
        public static void MessageRemoveAnimation(FrameworkElement element, bool isOutgoing, bool removeFromParent = true)
        {
            if (element == null)
                return;
            
            // Для исходящих сообщений анимируем вправо
            // Для входящих - влево
            double toX = isOutgoing ? 100 : -100;
            
            // Создаем анимацию сдвига с пропаданием прозрачности
            SlideAnimation(element, 0, toX, 0, 0, 200);
            FadeOut(element, 200, removeFromParent);
        }
        
        /// <summary>
        /// Создает и запускает анимацию пульсации для элемента (используется для выделения нового сообщения)
        /// </summary>
        /// <param name="element">Элемент для анимации</param>
        public static void PulseAnimation(FrameworkElement element)
        {
            if (element == null)
                return;
            
            // Сохраняем текущую трансформацию
            Transform originalTransform = element.RenderTransform;
            Point originalCenter = element.RenderTransformOrigin;
            
            // Устанавливаем центр трансформации
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            
            // Создаем группу трансформаций
            TransformGroup group = new TransformGroup();
            ScaleTransform scaleTransform = new ScaleTransform();
            group.Children.Add(scaleTransform);
            
            // Если была оригинальная трансформация, добавляем ее
            if (originalTransform != null && !(originalTransform is TransformGroup))
            {
                group.Children.Add(originalTransform);
            }
            
            element.RenderTransform = group;
            
            // Анимация масштабирования
            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            
            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            
            // Запускаем анимации
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            
            // Восстанавливаем оригинальные значения после анимации
            scaleXAnimation.Completed += (s, e) =>
            {
                element.RenderTransform = originalTransform;
                element.RenderTransformOrigin = originalCenter;
            };
        }
        
        /// <summary>
        /// Создает и запускает анимацию встряхивания для элемента (используется для уведомления об ошибке)
        /// </summary>
        /// <param name="element">Элемент для анимации</param>
        public static void ShakeAnimation(FrameworkElement element)
        {
            if (element == null)
                return;
            
            // Сохраняем текущую трансформацию
            Transform originalTransform = element.RenderTransform;
            
            // Создаем трансформацию сдвига
            TranslateTransform transform = new TranslateTransform();
            element.RenderTransform = transform;
            
            // Определяем ключевые кадры для анимации
            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            
            // Добавляем ключевые кадры (сдвиг влево и вправо)
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
            
            // После анимации восстанавливаем оригинальную трансформацию
            animation.Completed += (s, e) => element.RenderTransform = originalTransform;
            
            // Запускаем анимацию
            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
        
        /// <summary>
        /// Создает и запускает анимацию мигания фона (для уведомления о новом сообщении)
        /// </summary>
        /// <param name="element">Элемент для анимации</param>
        /// <param name="fromColor">Начальный цвет</param>
        /// <param name="toColor">Конечный цвет</param>
        /// <param name="duration">Продолжительность анимации в миллисекундах</param>
        /// <param name="repeat">Количество повторений (-1 для бесконечного повторения)</param>
        public static void BackgroundColorAnimation(FrameworkElement element, Color fromColor, Color toColor, double duration = 1000, int repeat = 1)
        {
            if (element == null)
                return;
            
            // Создаем новую кисть для заднего фона
            SolidColorBrush brush = new SolidColorBrush(fromColor);
            
            // Проверяем тип элемента и устанавливаем фон только для поддерживаемых типов
            if (element is Panel panel)
            {
                panel.Background = brush;
            }
            else if (element is Border border)
            {
                border.Background = brush;
            }
            else if (element is Control control)
            {
                control.Background = brush;
            }
            else
            {
                // Для неподдерживаемых типов выводим сообщение и выходим
                System.Diagnostics.Debug.WriteLine($"Тип {element.GetType().Name} не поддерживает свойство Background");
                return;
            }
            
            // Создаем анимацию цвета
            ColorAnimation animation = new ColorAnimation
            {
                From = fromColor,
                To = toColor,
                Duration = TimeSpan.FromMilliseconds(duration),
                AutoReverse = true
            };
            
            // Настраиваем повторение
            if (repeat > 0)
            {
                animation.RepeatBehavior = new RepeatBehavior(repeat);
            }
            else if (repeat < 0)
            {
                animation.RepeatBehavior = RepeatBehavior.Forever;
            }
            
            // Запускаем анимацию
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
} 