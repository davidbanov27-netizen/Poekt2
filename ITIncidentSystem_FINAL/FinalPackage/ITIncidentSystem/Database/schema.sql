-- ═══════════════════════════════════════════════════════════════════════════
-- IT Incident Management System — MySQL Schema
-- Седмица 3: База данни
-- ═══════════════════════════════════════════════════════════════════════════
-- Изпълни: mysql -u root -p < schema.sql
-- ═══════════════════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS it_incidents
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE it_incidents;

-- ── Изтриване на стари таблици (в правилен ред заради FK) ─────────────────
DROP TABLE IF EXISTS incident_history;
DROP TABLE IF EXISTS incidents;
DROP TABLE IF EXISTS technicians;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS departments;

-- ═══════════════════════════════════════════════════════════════════════════
-- ТАБЛИЦА: departments
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE departments (
    department_id  INT           NOT NULL AUTO_INCREMENT,
    name           VARCHAR(100)  NOT NULL UNIQUE,
    manager_name   VARCHAR(150)  NOT NULL,
    employee_count INT           NOT NULL DEFAULT 0,
    created_at     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (department_id),
    CONSTRAINT chk_dept_employees CHECK (employee_count >= 0)
) ENGINE=InnoDB;

-- ═══════════════════════════════════════════════════════════════════════════
-- ТАБЛИЦА: technicians
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE technicians (
    technician_id  INT          NOT NULL AUTO_INCREMENT,
    full_name      VARCHAR(150) NOT NULL,
    email          VARCHAR(200) NOT NULL UNIQUE,
    specialization ENUM('Hardware','Software','Network','Peripheral','Security','Other')
                                NOT NULL DEFAULT 'Other',
    is_available   TINYINT(1)  NOT NULL DEFAULT 1,
    active_cases   INT         NOT NULL DEFAULT 0,
    created_at     DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (technician_id),
    CONSTRAINT chk_tech_cases CHECK (active_cases >= 0)
) ENGINE=InnoDB;

-- ═══════════════════════════════════════════════════════════════════════════
-- ТАБЛИЦА: users
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE users (
    user_id       INT          NOT NULL AUTO_INCREMENT,
    full_name     VARCHAR(150) NOT NULL,
    email         VARCHAR(200) NOT NULL UNIQUE,
    phone         VARCHAR(20)           DEFAULT NULL,
    department_id INT          NOT NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (user_id),
    CONSTRAINT fk_user_dept FOREIGN KEY (department_id)
        REFERENCES departments (department_id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- ═══════════════════════════════════════════════════════════════════════════
-- ТАБЛИЦА: incidents (главна таблица)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE incidents (
    incident_id      INT          NOT NULL AUTO_INCREMENT,
    title            VARCHAR(200) NOT NULL,
    description      TEXT         NOT NULL,
    category         ENUM('Hardware','Software','Network','Peripheral','Security','Other')
                                  NOT NULL,
    priority         ENUM('Low','Medium','High','Critical')
                                  NOT NULL,
    status           ENUM('Open','InProgress','Resolved','Closed','Cancelled')
                                  NOT NULL DEFAULT 'Open',
    user_id          INT          NOT NULL,
    technician_id    INT                   DEFAULT NULL,
    department_id    INT          NOT NULL,
    resolution_notes TEXT                  DEFAULT NULL,
    created_at       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_at      DATETIME              DEFAULT NULL,
    resolved_at      DATETIME              DEFAULT NULL,

    PRIMARY KEY (incident_id),
    CONSTRAINT fk_inc_user  FOREIGN KEY (user_id)
        REFERENCES users (user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_inc_tech  FOREIGN KEY (technician_id)
        REFERENCES technicians (technician_id) ON DELETE SET NULL,
    CONSTRAINT fk_inc_dept  FOREIGN KEY (department_id)
        REFERENCES departments (department_id) ON DELETE RESTRICT,

    -- Индекси за честите LINQ заявки
    INDEX idx_status   (status),
    INDEX idx_priority (priority),
    INDEX idx_category (category),
    INDEX idx_tech     (technician_id),
    INDEX idx_created  (created_at)
) ENGINE=InnoDB;

-- ═══════════════════════════════════════════════════════════════════════════
-- ТАБЛИЦА: incident_history (одиторска следа)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE incident_history (
    history_id   INT          NOT NULL AUTO_INCREMENT,
    incident_id  INT          NOT NULL,
    changed_by   VARCHAR(150) NOT NULL,
    old_status   ENUM('Open','InProgress','Resolved','Closed','Cancelled')
                              DEFAULT NULL,
    new_status   ENUM('Open','InProgress','Resolved','Closed','Cancelled')
                              NOT NULL,
    notes        TEXT                  DEFAULT NULL,
    changed_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (history_id),
    CONSTRAINT fk_hist_inc FOREIGN KEY (incident_id)
        REFERENCES incidents (incident_id) ON DELETE CASCADE,

    INDEX idx_hist_incident (incident_id),
    INDEX idx_hist_changed  (changed_at)
) ENGINE=InnoDB;


-- ═══════════════════════════════════════════════════════════════════════════
-- SEED ДАННИ
-- ═══════════════════════════════════════════════════════════════════════════

-- ── Отдели ────────────────────────────────────────────────────────────────
INSERT INTO departments (name, manager_name, employee_count) VALUES
('Финансов отдел',   'Иван Петров',       24),
('Отдел Продажби',   'Мария Колева',      31),
('ИТ отдел',         'Стефан Димитров',   12),
('HR отдел',         'Елена Иванова',      8),
('Маркетинг',        'Николай Стоянов',   15),
('Юридически отдел', 'Биляна Тодорова',    6);

-- ── Техници ───────────────────────────────────────────────────────────────
INSERT INTO technicians (full_name, email, specialization, is_available, active_cases) VALUES
('Александър Георгиев', 'a.georgiev@company.bg',  'Hardware',  1, 0),
('Даниел Маринов',      'd.marinov@company.bg',   'Software',  1, 0),
('Кристина Василева',   'k.vasileva@company.bg',  'Network',   1, 0),
('Мартин Тодоров',      'm.todorov@company.bg',   'Security',  1, 0),
('Симона Христова',     's.hristova@company.bg',  'Hardware',  1, 0);

-- ── Потребители ───────────────────────────────────────────────────────────
INSERT INTO users (full_name, email, phone, department_id) VALUES
('Петя Атанасова',    'p.atanasova@company.bg',  '0888-111-001', 1),
('Борис Недялков',    'b.nedyalkov@company.bg',   '0888-111-002', 2),
('Галина Стоева',     'g.stoeva@company.bg',      '0888-111-003', 3),
('Радослав Иванов',   'r.ivanov@company.bg',      '0888-111-004', 4),
('Теодора Любенова',  't.lyubenova@company.bg',   '0888-111-005', 5),
('Велин Костов',      'v.kostov@company.bg',      '0888-111-006', 6),
('Надежда Василева',  'n.vasileva@company.bg',    '0888-111-007', 1),
('Стоян Маринов',     's.marinov@company.bg',     '0888-111-008', 2),
('Ивелина Тодорова',  'i.todorova@company.bg',    '0888-111-009', 3),
('Красимир Петков',   'k.petkov@company.bg',      '0888-111-010', 4);

-- ═══════════════════════════════════════════════════════════════════════════
-- 30 ТЕСТОВИ ИНЦИДЕНТА
-- ═══════════════════════════════════════════════════════════════════════════

INSERT INTO incidents
    (title, description, category, priority, status,
     user_id, technician_id, department_id,
     resolution_notes, created_at, assigned_at, resolved_at)
VALUES

-- ── Решени инциденти (Resolved) ───────────────────────────────────────────
('Компютър не се включва — счетоводство',
 'Работната станция на Петя Атанасова не реагира при натискане на бутона за включване. Проверен захранващия кабел — няма промяна.',
 'Hardware','High','Resolved', 1,1,1,
 'Сменен дефектен захранващ блок (PSU). Компютърът работи нормално.',
 '2025-09-01 08:15:00','2025-09-01 09:00:00','2025-09-01 11:30:00'),

('Excel не стартира след Windows Update',
 'След автоматичния ъпдейт от миналата нощ Microsoft Excel показва грешка "Application failed to initialize" при стартиране.',
 'Software','Medium','Resolved', 2,2,2,
 'Деинсталиран проблемният KB5034441 пач. Excel работи нормално. Добавено изключение за Office.',
 '2025-09-01 09:30:00','2025-09-01 10:15:00','2025-09-01 13:00:00'),

('Без интернет — целия 3-ти етаж',
 'От 08:00 целият 3-ти етаж (Продажби) е без интернет свързаност. Засегнати са ~30 служители.',
 'Network','Critical','Resolved', 2,3,2,
 'Рестартиран достъпен суич. Причина: firmware bug при автоматична актуализация. Обновен firmware.',
 '2025-09-02 08:05:00','2025-09-02 08:20:00','2025-09-02 09:45:00'),

('Принтерът в HR не печата цветно',
 'HP Color LaserJet в HR отдела печата само в черно-бяло. Цветните касети са нови, сменени преди 2 седмици.',
 'Peripheral','Low','Resolved', 4,5,4,
 'Почистени дюзи. Проблемът е бил в запушена жълта дюза. Тест страницата е наред.',
 '2025-09-02 10:00:00','2025-09-03 09:00:00','2025-09-03 11:00:00'),

('Подозрителни опити за вход — Финансова система',
 'Системата е регистрирала 847 неуспешни опита за вход към финансовата апликация от IP 185.220.101.45 за 10 минути.',
 'Security','Critical','Resolved', 1,4,1,
 'IP блокиран на ниво firewall. Всички пароли на Финансов отдел са нулирани. Активиран 2FA.',
 '2025-09-03 07:55:00','2025-09-03 08:00:00','2025-09-03 08:30:00'),

('Монитор с черни хоризонтални ивици',
 'Мониторът на Борис Недялков показва черни ивици в долната трета. Проблемът е постоянен, не зависи от ъгъла.',
 'Hardware','Medium','Resolved', 2,1,2,
 'Сменен монитор с резервен от склада. Дефектният изпратен на производителя по гаранция.',
 '2025-09-03 11:00:00','2025-09-04 09:30:00','2025-09-04 10:15:00'),

('Microsoft Teams не може да споделя екран',
 'При опит за споделяне на екран в Teams срещата зависва. Проблемът е при всички потребители в Маркетинг.',
 'Software','Medium','Resolved', 5,2,5,
 'Актуализирана версия на Teams (2.1.00.34161). Проблемът е бил известен bug в предишната версия.',
 '2025-09-04 09:00:00','2025-09-04 10:00:00','2025-09-04 14:30:00'),

('VPN не се свързва от домашен офис',
 'Служители на Финансов отдел не могат да се свържат с VPN за отдалечена работа. Грешка: "Authentication failed".',
 'Network','High','Resolved', 1,3,1,
 'Актуализиран VPN сертификат (изтекъл). Всички потребители уведомени с инструкции за преинсталация.',
 '2025-09-05 08:30:00','2025-09-05 09:00:00','2025-09-05 11:00:00'),

('Мишка с прекъснат USB кабел',
 'USB кабелът на мишката на Радослав Иванов е физически прекъснат в основата. Мишката работи само ако кабелът е наклонен.',
 'Peripheral','Low','Resolved', 4,5,4,
 'Заменена с нова безжична мишка от склада.',
 '2025-09-05 10:30:00','2025-09-08 09:00:00','2025-09-08 09:30:00'),

('Ransomware подозрение — работна станция Маркетинг',
 'Антивирусът е засякъл подозрителна активност на работната станция на Теодора. Множество файлове се опитват да се криптират.',
 'Security','Critical','Resolved', 5,4,5,
 'Изолирана засегнатата машина. Извършено пълно сканиране. Заразени 3 файла — изтрити. Преинсталирана ОС.',
 '2025-09-08 11:00:00','2025-09-08 11:05:00','2025-09-08 16:00:00'),

('Сървър DB01 — дисково пространство 95%',
 'RAID масивът на основния DB сървър е запълнен на 95%. Очаква се пълно запълване до 48 часа.',
 'Hardware','Critical','Resolved', 3,1,3,
 'Преместени архивни логове на NAS. Добавен 2TB HDD. Пространство намалено до 67%.',
 '2025-09-09 07:00:00','2025-09-09 07:15:00','2025-09-09 10:00:00'),

('Windows Update блокира работна станция',
 'Работната станция на Велин Костов е заседнала при 35% от Windows Update от вчера. Не реагира.',
 'Software','High','Resolved', 6,2,6,
 'Принудително спряна актуализацията. Изчистен Windows Update кеш. Актуализация завършена успешно.',
 '2025-09-09 09:00:00','2025-09-09 09:30:00','2025-09-09 11:30:00'),

('Мрежов суич — порт 12 изгорял',
 'Порт 12 на суича в сървърното помещение не работи. 4 сървъра са без мрежа.',
 'Network','High','Resolved', 3,3,3,
 'Преместени кабелите на свободни портове. Поръчан резервен суич. Временно решение работи стабилно.',
 '2025-09-10 08:00:00','2025-09-10 08:30:00','2025-09-10 12:30:00'),

('Скенерът не разпознава документи коректно',
 'Canon скенерът в счетоводството разпознава документи наклонено — OCR дава грешни резултати.',
 'Peripheral','Medium','Resolved', 1,5,1,
 'Почистено стъклото. Калибриран скенерът. OCR accuracy се е върнала до 98%.',
 '2025-09-10 10:00:00','2025-09-11 09:00:00','2025-09-11 10:30:00'),

('Изтекла парола на домейн акаунт',
 'Надежда Василева не може да влезе в компютъра. Паролата е изтекла и системата не предлага опция за смяна.',
 'Security','Medium','Resolved', 7,4,1,
 'Паролата е нулирана от Active Directory. Потребителят е обучен как да сменя паролата навреме.',
 '2025-09-11 08:45:00','2025-09-11 09:00:00','2025-09-11 09:30:00'),

('Клавиатура — залята с кафе',
 'Стоян Маринов е разлял кафе върху клавиатурата. Половината клавиши не работят.',
 'Hardware','Low','Resolved', 8,5,2,
 'Клавиатурата е тотална загуба. Заменена с нова механична клавиатура.',
 '2025-09-12 10:15:00','2025-09-12 11:00:00','2025-09-12 11:30:00'),

('ERP системата се рестартира сама на всеки 2 часа',
 'Продуктовата ERP система (SAP) се рестартира автоматично на всеки ~2 часа. Всички потребители губят несъхранените данни.',
 'Software','High','Resolved', 3,2,3,
 'Открита memory leak в custom модул. Приложен hotfix от SAP. Системата работи стабилно 48 часа.',
 '2025-09-15 07:30:00','2025-09-15 08:00:00','2025-09-18 09:00:00'),

('Wi-Fi сигнал много слаб в конферентната зала',
 'В конферентната зала на 2-ри етаж Wi-Fi сигналът е 1 лента. Online срещите прекъсват постоянно.',
 'Network','Medium','Resolved', 5,3,5,
 'Добавена нова точка за достъп (AP) в конферентната зала. Сигналът е 5 ленти.',
 '2025-09-15 09:00:00','2025-09-16 09:00:00','2025-09-17 14:00:00'),

('Проекторът в зала 2 не показва HDMI сигнал',
 'При свързване на лаптоп с HDMI кабел проекторът показва "No Signal". VGA работи нормално.',
 'Peripheral','Medium','Resolved', 9,5,3,
 'Сменен дефектен HDMI адаптер. Проекторът разпознава HDMI нормално.',
 '2025-09-16 13:00:00','2025-09-16 14:00:00','2025-09-16 15:00:00'),

('Открит USB флаш с malware при входяща поща',
 'Намерен USB флаш в пощата на компанията с адрес до ИТ отдела. При анализ — открит Trojan.Agent.',
 'Security','Critical','Resolved', 3,4,3,
 'USB унищожен. Сканирани всички системи, до които е имал достъп. Не е установено заразяване.',
 '2025-09-17 09:00:00','2025-09-17 09:05:00','2025-09-17 11:00:00'),

-- ── Инциденти в процес (InProgress) ───────────────────────────────────────
('Работна станция — BSOD при стартиране на AutoCAD',
 'Ивелина Тодорова получава Blue Screen of Death (MEMORY_MANAGEMENT) при всеки опит за стартиране на AutoCAD 2024.',
 'Hardware','High','InProgress', 9,1,3,
 NULL,
 '2025-09-19 08:00:00','2025-09-19 09:00:00',NULL),

('Outlook не синхронизира контакти с телефона',
 'Контактите в Outlook на Красимир Петков не се синхронизират с корпоративния телефон. Последна синхронизация — преди 5 дни.',
 'Software','Low','InProgress', 10,2,4,
 NULL,
 '2025-09-19 10:30:00','2025-09-20 09:00:00',NULL),

('Вътрешен firewall блокира ERP апликацията',
 'След вчерашната firewall актуализация ERP системата е недостъпна от подмрежа 192.168.10.0/24.',
 'Network','High','InProgress', 3,3,3,
 NULL,
 '2025-09-20 08:15:00','2025-09-20 08:30:00',NULL),

('UPS — не задържа батерия при токов удар',
 'UPS устройството в сървърното помещение не задържа товара при симулиран токов удар. Батерията е на 8 години.',
 'Hardware','Medium','InProgress', 3,1,3,
 NULL,
 '2025-09-20 11:00:00','2025-09-21 09:00:00',NULL),

-- ── Отворени инциденти (Open — чакат назначаване) ─────────────────────────
('Zoom срива компютъра при споделяне на видео',
 'При включване на камерата в Zoom срещи компютърът на Надежда Василева замръзва напълно след 2-3 минути.',
 'Software','Medium','Open', 7,NULL,1,
 NULL, '2025-09-21 09:00:00',NULL,NULL),

('IP адрес конфликт — 2 принтера с един IP',
 'Двата принтера на 1-ви етаж са получили един и същ IP адрес след рестарт на DHCP сървъра. Нито един не печата.',
 'Network','High','Open', 8,NULL,2,
 NULL, '2025-09-21 10:30:00',NULL,NULL),

('Принтер — засядане на хартия при всяка 3-та страница',
 'Принтерът в юридическия отдел засяда при всяка трета отпечатана страница. Ролките изглеждат износени.',
 'Peripheral','Low','Open', 6,NULL,6,
 NULL, '2025-09-22 08:00:00',NULL,NULL),

('Изтекъл SSL сертификат на корпоративния сайт',
 'SSL сертификатът на www.company.bg е изтекъл. Браузърите показват предупреждение "Your connection is not private".',
 'Security','Critical','Open', 3,NULL,3,
 NULL, '2025-09-22 07:00:00',NULL,NULL),

('RAM памет — чести сривове при рендиране',
 'Работната станция на дизайнера в Маркетинг срива при рендиране на видео. Диагностиката показва грешки в RAM.',
 'Hardware','High','Open', 5,NULL,5,
 NULL, '2025-09-22 11:00:00',NULL,NULL),

('CRM системата работи изключително бавно',
 'CRM платформата (Salesforce) е станала изключително бавна след последния release. Зарежда се по 15-20 секунди.',
 'Software','Medium','Open', 2,NULL,2,
 NULL, '2025-09-23 09:00:00',NULL,NULL);

-- ═══════════════════════════════════════════════════════════════════════════
-- ИСТОРИЯ НА ПРОМЕНИТЕ (за решените инциденти)
-- ═══════════════════════════════════════════════════════════════════════════

INSERT INTO incident_history (incident_id, changed_by, old_status, new_status, notes, changed_at)
VALUES
-- INC-1
(1, 'SYSTEM',               NULL,         'Open',       'Инцидентът е регистриран',                             '2025-09-01 08:15:00'),
(1, 'Стефан Димитров',     'Open',       'InProgress', 'Назначен: Александър Георгиев',                        '2025-09-01 09:00:00'),
(1, 'Александър Георгиев', 'InProgress', 'Resolved',   'Сменен PSU. Тест успешен.',                            '2025-09-01 11:30:00'),

-- INC-2
(2, 'SYSTEM',          NULL,         'Open',       'Инцидентът е регистриран',                                 '2025-09-01 09:30:00'),
(2, 'Стефан Димитров', 'Open',       'InProgress', 'Назначен: Даниел Маринов',                                 '2025-09-01 10:15:00'),
(2, 'Даниел Маринов',  'InProgress', 'Resolved',   'Деинсталиран проблемен пач.',                              '2025-09-01 13:00:00'),

-- INC-3 (Critical)
(3, 'SYSTEM',           NULL,         'Open',       'КРИТИЧЕН — изпратена аларма до мениджмънта',              '2025-09-02 08:05:00'),
(3, 'Стефан Димитров',  'Open',       'InProgress', 'Назначен: Кристина Василева. Приоритет: незабавно',       '2025-09-02 08:20:00'),
(3, 'Кристина Василева','InProgress', 'Resolved',   'Рестартиран суич. Firmware актуализиран.',                '2025-09-02 09:45:00'),

-- INC-5 (Security Critical)
(5, 'SYSTEM',        NULL,         'Open',       'SECURITY CRITICAL — ескалирано до CISO',                     '2025-09-03 07:55:00'),
(5, 'Стефан Димитров','Open',      'InProgress', 'Назначен: Мартин Тодоров. Незабавна реакция.',               '2025-09-03 08:00:00'),
(5, 'Мартин Тодоров', 'InProgress','Resolved',   'IP блокиран. Пароли нулирани. 2FA активиран.',               '2025-09-03 08:30:00'),

-- INC-10 (Ransomware)
(10, 'SYSTEM',        NULL,         'Open',       'RANSOMWARE ALERT — изолирана машина',                       '2025-09-08 11:00:00'),
(10, 'Стефан Димитров','Open',      'InProgress', 'Назначен: Мартин Тодоров. Форензик анализ.',                '2025-09-08 11:05:00'),
(10, 'Мартин Тодоров', 'InProgress','Resolved',   'Зараза ограничена. ОС преинсталирана.',                     '2025-09-08 16:00:00'),

-- INC-21 (InProgress)
(21, 'SYSTEM',         NULL,   'Open',       'Инцидентът е регистриран',                                       '2025-09-19 08:00:00'),
(21, 'Стефан Димитров','Open', 'InProgress', 'Назначен: Александър Георгиев. RAM тест в процес.',              '2025-09-19 09:00:00'),

-- INC-28 (SSL — Critical Open)
(28, 'SYSTEM', NULL, 'Open', 'КРИТИЧЕН — SSL изтекъл. Сайтът е недостъпен за клиенти.', '2025-09-22 07:00:00');


-- ═══════════════════════════════════════════════════════════════════════════
-- ПОЛЕЗНИ VIEW-ТА
-- ═══════════════════════════════════════════════════════════════════════════

CREATE OR REPLACE VIEW v_incidents_full AS
SELECT
    i.incident_id,
    CONCAT('INC-', LPAD(i.incident_id, 5, '0')) AS incident_number,
    i.title,
    i.category,
    i.priority,
    i.status,
    u.full_name        AS user_name,
    u.email            AS user_email,
    t.full_name        AS technician_name,
    d.name             AS department_name,
    i.created_at,
    i.assigned_at,
    i.resolved_at,
    TIMESTAMPDIFF(MINUTE, i.created_at, i.resolved_at) AS resolution_minutes,
    i.resolution_notes
FROM incidents i
JOIN users        u ON i.user_id       = u.user_id
JOIN departments  d ON i.department_id = d.department_id
LEFT JOIN technicians t ON i.technician_id = t.technician_id;


CREATE OR REPLACE VIEW v_technician_stats AS
SELECT
    t.technician_id,
    t.full_name,
    t.specialization,
    t.is_available,
    t.active_cases,
    COUNT(i.incident_id)                                        AS total_incidents,
    SUM(i.status = 'Resolved' OR i.status = 'Closed')          AS resolved_count,
    AVG(TIMESTAMPDIFF(MINUTE, i.created_at, i.resolved_at))    AS avg_resolution_min
FROM technicians t
LEFT JOIN incidents i ON t.technician_id = i.technician_id
GROUP BY t.technician_id;


-- ═══════════════════════════════════════════════════════════════════════════
-- ПРОВЕРКА
-- ═══════════════════════════════════════════════════════════════════════════
SELECT 'Departments' AS tbl, COUNT(*) AS cnt FROM departments
UNION ALL SELECT 'Technicians', COUNT(*) FROM technicians
UNION ALL SELECT 'Users',       COUNT(*) FROM users
UNION ALL SELECT 'Incidents',   COUNT(*) FROM incidents
UNION ALL SELECT 'History',     COUNT(*) FROM incident_history;
