# ElectricTrainsPlugin

BepInEx mod for the game **Electric Trains** — adds quality-of-life features and cheats.

BepInEx мод для игры **Electric Trains** — улучшения и читы.

---

## Requirements / Требования

- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases)

---

## Installation / Установка

1. Install BepInEx into the game folder / Установите BepInEx в папку с игрой
2. Copy `ElectricTrainsPlugin.dll` to `BepInEx/plugins/` / Скопируйте `ElectricTrainsPlugin.dll` в `BepInEx/plugins/`
3. Launch the game / Запустите игру

---

## Features / Возможности

### 1. Acceleration / Ускорение
Multipliers for throttle and brake force.  
Множители тяги и тормозной силы.

### 2. No Derail / Без схода с рельс
Disables train derailment.  
Отключает сход поезда с рельс.

### 3. Transparent Red Barriers / Прозрачные красные барьеры
Allows passing through red blocker barriers.  
Позволяет проезжать сквозь красные барьеры-тупики.

### 4. Infinite Points / Бесконечные очки
Sets available points to 999999999.  
Устанавливает количество очков в 999999999.

### 5. No Traffic / Отключение трафика
Disables oncoming trains.  
Отключает встречные поезда.

### 6. Free Camera / Свободная камера
Expands camera zoom and height limits.  
Расширяет лимиты зума и высоты камеры.

### 7. FPS Unlock / Анлок FPS
Removes the frame rate cap.  
Снимает ограничение частоты кадров.

### 8. Smooth Cruise Control / Плавный круиз-контроль
Smooths out cruise control acceleration changes.  
Сглаживает резкие изменения тяги при круиз-контроле.

### 9. Map Teleport / Телепорт по карте
**Right-click** on the minimap to teleport the train to that location.  
**ПКМ** по миникарте телепортирует поезд в выбранную точку.

---

## Configuration / Настройка

All options are configurable via `BepInEx/config/com.electrictrains.mods.cfg` or with [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (F1 in-game).

Все параметры настраиваются через `BepInEx/config/com.electrictrains.mods.cfg` или через [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (F1 в игре).

---

## Notes / Примечания

- Map teleport places the train at the nearest track point to the click — accuracy depends on track segment density.  
  Телепорт по карте ставит поезд в ближайшую точку пути — точность зависит от плотности точек на участке.
- If teleported to the wrong spot, stay on the map and right-click again to correct.  
  Если попали не туда — не закрывайте карту, кликните ПКМ ещё раз.

---

## License / Лицензия

MIT
