using System;

public enum QuizDifficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

[Serializable]
public class QuizQuestion
{
    public string category;
    public string questionText;
    public string[] choices;
    public int correctIndex;
    public string explanation;

    public QuizQuestion(string category, string questionText, string a, string b, string c, string d, int correctIndex)
        : this(category, questionText, a, b, c, d, correctIndex, "")
    {
    }

    public QuizQuestion(string category, string questionText, string a, string b, string c, string d, int correctIndex, string explanation)
    {
        this.category = category;
        this.questionText = questionText;
        this.choices = new string[] { a, b, c, d };
        this.correctIndex = correctIndex;
        this.explanation = explanation;
    }
}

/// <summary>
/// Holds Easy, Medium, and Hard educational questions across Math, Science, English, Filipino, and General Knowledge,
/// including concise 1-line explanations for learning feedback.
/// </summary>
public static class QuestionBank
{
    public static QuizQuestion[] GetQuestions(QuizDifficulty difficulty)
    {
        switch (difficulty)
        {
            case QuizDifficulty.Medium:
                return GetMediumQuestions();
            case QuizDifficulty.Hard:
                return GetHardQuestions();
            default:
                return GetEasyQuestions();
        }
    }

    // 30 Easy Elementary Questions (6 per category)
    private static QuizQuestion[] GetEasyQuestions()
    {
        return new QuizQuestion[]
        {
            // MATHEMATICS
            new QuizQuestion("Mathematics", "What is 5 + 3?", "6", "8", "9", "10", 1, "Counting 3 steps up from 5 gives 8."),
            new QuizQuestion("Mathematics", "What is 10 - 4?", "4", "5", "6", "7", 2, "Taking 4 away from 10 leaves 6."),
            new QuizQuestion("Mathematics", "What is 2 x 3?", "5", "6", "7", "8", 1, "Two groups of 3 equal 6."),
            new QuizQuestion("Mathematics", "What is 12 / 3?", "2", "3", "4", "5", 2, "12 split equally into 3 groups gives 4 in each."),
            new QuizQuestion("Mathematics", "What is 7 + 2?", "8", "9", "10", "11", 1, "Adding 2 to 7 makes 9."),
            new QuizQuestion("Mathematics", "What is 5 x 2?", "10", "12", "15", "20", 0, "Two 5s added together equal 10."),

            // SCIENCE
            new QuizQuestion("Science", "What do plants need to grow?", "Water", "Plastic", "Stone", "Glass", 0, "Plants need water, sunlight, and air to grow."),
            new QuizQuestion("Science", "Which animal can fly?", "Dog", "Cat", "Bird", "Fish", 2, "Birds use feathered wings to fly."),
            new QuizQuestion("Science", "What do we use to see?", "Ears", "Eyes", "Nose", "Hands", 1, "Our eyes sense light so we can see."),
            new QuizQuestion("Science", "What gives us light during the day?", "Moon", "Sun", "Star", "Cloud", 1, "The Sun illuminates Earth during daytime."),
            new QuizQuestion("Science", "What do fish use to breathe?", "Lungs", "Gills", "Wings", "Feet", 1, "Fish absorb oxygen from water using gills."),
            new QuizQuestion("Science", "What do we drink to stay healthy?", "Soda", "Coffee", "Water", "Juice", 2, "Clean water keeps the body hydrated and healthy."),

            // ENGLISH
            new QuizQuestion("English", "What is the opposite of hot?", "Cold", "Warm", "Big", "Fast", 0, "Cold is the direct antonym of hot."),
            new QuizQuestion("English", "Which word is a color?", "Apple", "Blue", "Table", "Chair", 1, "Blue names a color, while the others are objects."),
            new QuizQuestion("English", "What is the plural of cat?", "Cats", "Cates", "Cat's", "Caties", 0, "Most nouns form their plural by adding -s."),
            new QuizQuestion("English", "Which animal says meow?", "Dog", "Cat", "Cow", "Duck", 1, "Cats make the 'meow' sound."),
            new QuizQuestion("English", "What is the opposite of big?", "Small", "Tall", "Long", "Heavy", 0, "Small means the opposite size of big."),
            new QuizQuestion("English", "Which word is a fruit?", "Carrot", "Mango", "Chair", "Book", 1, "A mango is a sweet tropical fruit."),

            // FILIPINO
            new QuizQuestion("Filipino", "Ano ang kulay ng dugo?", "Pula", "Asul", "Dilaw", "Berde", 0, "Ang dugo ng tao ay kulay pula dahil sa hemoglobin."),
            new QuizQuestion("Filipino", "Ano ang kabaligtaran ng taas?", "Baba", "Laki", "Liit", "Init", 0, "Ang baba ay kasalungat ng taas."),
            new QuizQuestion("Filipino", "Ilang araw mayroon sa isang linggo?", "5", "6", "7", "8", 2, "May pitong araw mula Lunes hanggang Linggo."),
            new QuizQuestion("Filipino", "Ano ang tawag sa ina ng iyong ina?", "Lola", "Ate", "Tita", "Pinsan", 0, "Lola ang tawag sa nanay ng ating nanay o tatay."),
            new QuizQuestion("Filipino", "Ano ang pambansang wika ng Pilipinas?", "English", "Filipino", "Spanish", "French", 1, "Filipino ang opisyal na pambansang wika ng Pilipinas."),
            new QuizQuestion("Filipino", "Ano ang kulay ng dahon?", "Asul", "Pula", "Berde", "Itim", 2, "Ang mga dahon ay berde dahil sa chlorophyll."),

            // GENERAL KNOWLEDGE
            new QuizQuestion("General Knowledge", "How many days are in a week?", "5", "6", "7", "8", 2, "Every calendar week has 7 days."),
            new QuizQuestion("General Knowledge", "How many months are in a year?", "10", "11", "12", "13", 2, "A full year runs across 12 months from January to December."),
            new QuizQuestion("General Knowledge", "What shape has three sides?", "Circle", "Square", "Triangle", "Rectangle", 2, "'Tri' means three, so a triangle has 3 sides."),
            new QuizQuestion("General Knowledge", "What is the first month of the year?", "January", "March", "June", "August", 0, "January opens the calendar year."),
            new QuizQuestion("General Knowledge", "How many legs does a dog have?", "2", "3", "4", "6", 2, "Dogs walk on 4 legs."),
            new QuizQuestion("General Knowledge", "What do we use to write?", "Spoon", "Pencil", "Plate", "Fork", 1, "A pencil is a writing tool.")
        };
    }

    // 30 Medium Questions (6 per category)
    private static QuizQuestion[] GetMediumQuestions()
    {
        return new QuizQuestion[]
        {
            // MATHEMATICS
            new QuizQuestion("Mathematics", "What is 15 x 4?", "45", "60", "55", "70", 1, "15 x 2 is 30, and doubling 30 gives 60."),
            new QuizQuestion("Mathematics", "What is 144 / 12?", "11", "12", "13", "14", 1, "12 multiplied by 12 equals 144."),
            new QuizQuestion("Mathematics", "What is 3/4 of 20?", "10", "12", "15", "16", 2, "One quarter of 20 is 5, so three quarters equal 15."),
            new QuizQuestion("Mathematics", "What is 7 squared (7 x 7)?", "42", "48", "49", "56", 2, "7 multiplied by itself equals 49."),
            new QuizQuestion("Mathematics", "What is 25% of 80?", "15", "20", "25", "40", 1, "25% is one quarter, and 80 divided by 4 is 20."),
            new QuizQuestion("Mathematics", "What is 36 + 47?", "73", "81", "83", "93", 2, "36 + 40 = 76, and 76 + 7 = 83."),

            // SCIENCE
            new QuizQuestion("Science", "What process do plants use to make food from sunlight?", "Respiration", "Photosynthesis", "Evaporation", "Digestion", 1, "Photosynthesis converts sunlight, water, and CO2 into sugar."),
            new QuizQuestion("Science", "Which planet is known as the Red Planet?", "Venus", "Mars", "Jupiter", "Saturn", 1, "Iron oxide dust on Mars's surface gives it a reddish color."),
            new QuizQuestion("Science", "At what temperature does water boil at sea level?", "50 C", "90 C", "100 C", "120 C", 2, "Pure water boils at 100 degrees Celsius at standard pressure."),
            new QuizQuestion("Science", "Which force pulls objects toward the Earth?", "Magnetism", "Friction", "Gravity", "Tension", 2, "Gravity pulls objects with mass toward Earth's center."),
            new QuizQuestion("Science", "Which organ pumps blood through the human body?", "Lungs", "Brain", "Heart", "Stomach", 2, "The muscular heart pumps blood through arteries and veins."),
            new QuizQuestion("Science", "What type of blood do mammals have?", "Cold-blooded", "Warm-blooded", "Blue blood", "No blood", 1, "Mammals are warm-blooded and regulate their own body heat."),

            // ENGLISH
            new QuizQuestion("English", "Which word is a verb (action word)?", "Quickly", "Run", "Happy", "Mountain", 1, "'Run' shows an action; the others are adverb, adjective, and noun."),
            new QuizQuestion("English", "What is a synonym for 'happy'?", "Sad", "Angry", "Joyful", "Tired", 2, "Synonyms share similar meanings; joyful means very happy."),
            new QuizQuestion("English", "What is the past tense of 'go'?", "Goed", "Going", "Went", "Gone", 2, "'Go' is an irregular verb whose past tense is 'went'."),
            new QuizQuestion("English", "Which word is an adjective?", "Brave", "Swim", "Table", "Slowly", 0, "'Brave' describes a noun, making it an adjective."),
            new QuizQuestion("English", "What is the antonym of 'ancient'?", "Old", "Modern", "Broken", "Heavy", 1, "Modern means up-to-date, the opposite of ancient."),
            new QuizQuestion("English", "Which word is a compound word?", "Sunbeam", "Running", "Careful", "Teacher", 0, "Sunbeam joins two complete words: sun + beam."),

            // FILIPINO
            new QuizQuestion("Filipino", "Sino ang pambansang bayani ng Pilipinas?", "Andres Bonifacio", "Jose Rizal", "Emilio Aguinaldo", "Apolinario Mabini", 1, "Si Dr. Jose Rizal ang kinikilalang pambansang bayani."),
            new QuizQuestion("Filipino", "Ano ang kabisera (capital) ng Pilipinas?", "Cebu", "Davao", "Maynila", "Baguio", 2, "Ang Lungsod ng Maynila ang kabisera ng Pilipinas."),
            new QuizQuestion("Filipino", "Ano ang pambansang bulaklak ng Pilipinas?", "Rosas", "Sampaguita", "Gumamela", "Orchid", 1, "Sampaguita ang pambansang bulaklak na simbolo ng kadalisayan."),
            new QuizQuestion("Filipino", "Ano ang kasingkahulugan ng salitang 'masaya'?", "Malungkot", "Maligaya", "Galit", "Takot", 1, "Ang 'maligaya' at 'masaya' ay parehong nangangahulugan ng tuwa."),
            new QuizQuestion("Filipino", "Ano ang tawag sa mga salitang nag-uugnay tulad ng 'at' o 'ngunit'?", "Pangngalan", "Pangatnig", "Pandiwa", "Pang-uri", 1, "Pangatnig ang nag-uugnay ng mga salita o sugnay."),
            new QuizQuestion("Filipino", "Ilan ang titik sa makabagong alpabetong Filipino?", "20", "26", "28", "30", 2, "Ang makabagong alpabetong Filipino ay may 28 na titik."),

            // GENERAL KNOWLEDGE
            new QuizQuestion("General Knowledge", "How many continents are there on Earth?", "5", "6", "7", "8", 2, "Earth has 7 continents: Asia, Africa, North/South America, Antarctica, Europe, Australia."),
            new QuizQuestion("General Knowledge", "Which is the largest ocean on Earth?", "Atlantic Ocean", "Indian Ocean", "Pacific Ocean", "Arctic Ocean", 2, "The Pacific Ocean covers nearly one-third of Earth's surface."),
            new QuizQuestion("General Knowledge", "Which direction does a standard compass needle point?", "North", "South", "East", "West", 0, "A magnetic compass needle aligns toward magnetic North."),
            new QuizQuestion("General Knowledge", "How many days are in a leap year?", "364", "365", "366", "367", 2, "A leap year adds February 29 for a total of 366 days."),
            new QuizQuestion("General Knowledge", "At what temperature does water freeze in Celsius?", "0 C", "10 C", "32 C", "-10 C", 0, "Water freezes into ice at 0 degrees Celsius (32 F)."),
            new QuizQuestion("General Knowledge", "How many minutes are in two hours?", "60", "90", "120", "150", 2, "With 60 minutes per hour, 2 hours equal 120 minutes.")
        };
    }

    // 30 Hard Questions (6 per category)
    private static QuizQuestion[] GetHardQuestions()
    {
        return new QuizQuestion[]
        {
            // MATHEMATICS
            new QuizQuestion("Mathematics", "What is the square root of 169?", "11", "12", "13", "14", 2, "13 x 13 equals 169."),
            new QuizQuestion("Mathematics", "What is 18 x 7?", "116", "124", "126", "132", 2, "(10 x 7) + (8 x 7) = 70 + 56 = 126."),
            new QuizQuestion("Mathematics", "What is the sum of interior angles in any triangle?", "90 degrees", "180 degrees", "270 degrees", "360 degrees", 1, "All three interior angles of a Euclidean triangle always total 180 degrees."),
            new QuizQuestion("Mathematics", "What is 2 to the 5th power (2^5)?", "16", "24", "32", "64", 2, "2 x 2 x 2 x 2 x 2 equals 32."),
            new QuizQuestion("Mathematics", "Which of these numbers is a prime number?", "21", "27", "29", "33", 2, "29 has only two positive divisors: 1 and 29."),
            new QuizQuestion("Mathematics", "What is the perimeter of a rectangle with length 9 and width 6?", "15", "24", "30", "54", 2, "Perimeter = 2 x (9 + 6) = 30."),

            // SCIENCE
            new QuizQuestion("Science", "What is the chemical symbol for Gold?", "Ag", "Au", "Gd", "Go", 1, "Au comes from 'aurum', the Latin word for gold."),
            new QuizQuestion("Science", "Which cell organelle is known as the powerhouse of the cell?", "Nucleus", "Ribosome", "Mitochondria", "Chloroplast", 2, "Mitochondria generate ATP energy for cellular work."),
            new QuizQuestion("Science", "Which gas do plants primarily absorb for photosynthesis?", "Oxygen", "Nitrogen", "Carbon dioxide", "Hydrogen", 2, "Plants take in carbon dioxide (CO2) and release oxygen."),
            new QuizQuestion("Science", "What is the largest organ of the human body?", "Liver", "Heart", "Skin", "Lungs", 2, "Skin is our largest external organ, protecting the entire body."),
            new QuizQuestion("Science", "Which gas makes up about 78% of Earth's atmosphere?", "Oxygen", "Nitrogen", "Carbon dioxide", "Argon", 1, "Earth's air is roughly 78% nitrogen and 21% oxygen."),
            new QuizQuestion("Science", "Where does light travel the fastest?", "Water", "Glass", "Air", "Vacuum", 3, "With zero particles to slow it down, light travels fastest in a vacuum."),

            // ENGLISH
            new QuizQuestion("English", "Which figure of speech compares two things using 'like' or 'as'?", "Metaphor", "Simile", "Hyperbole", "Personification", 1, "A simile uses 'like' or 'as' for explicit comparison."),
            new QuizQuestion("English", "What is an antonym for 'scarce'?", "Rare", "Abundant", "Tiny", "Empty", 1, "Abundant means plentiful, the opposite of scarce."),
            new QuizQuestion("English", "Which sentence uses correct subject-verb agreement?", "The crew are ready.", "The ships sails fast.", "The captain steers the ship.", "Neither of them know.", 2, "Singular subject 'captain' takes the singular verb 'steers'."),
            new QuizQuestion("English", "What does the prefix 'anti-' mean?", "Before", "Against", "Under", "Together", 1, "The prefix 'anti-' means against or opposite."),
            new QuizQuestion("English", "Which pair of words are homophones?", "Big / Large", "Sea / See", "Hot / Cold", "Ship / Boat", 1, "Homophones like 'sea' and 'see' sound identical but have different meanings."),
            new QuizQuestion("English", "In 'She whispered softly', what part of speech is 'softly'?", "Noun", "Adjective", "Adverb", "Pronoun", 2, "An adverb like 'softly' modifies the verb 'whispered'."),

            // FILIPINO
            new QuizQuestion("Filipino", "Sino ang may-akda ng nobelang 'Noli Me Tangere'?", "Francisco Balagtas", "Jose Rizal", "Marcelo H. del Pilar", "Graciano Lopez Jaena", 1, "Isinulat ni Dr. Jose Rizal ang Noli Me Tangere noong 1887."),
            new QuizQuestion("Filipino", "Ano ang pinakamahabang ilog sa Pilipinas?", "Ilog Pasig", "Ilog Cagayan", "Ilog Pampanga", "Ilog Agusan", 1, "Ang Ilog Cagayan sa Hilagang Luzon ang pinakamahabang ilog sa bansa."),
            new QuizQuestion("Filipino", "Ano ang pinakamataas na bundok sa Pilipinas?", "Bundok Pulag", "Bundok Mayon", "Bundok Apo", "Bundok Banahaw", 2, "Ang Bundok Apo sa Mindanao ang pinakamataas na taluktok (2,954 m)."),
            new QuizQuestion("Filipino", "Ano ang tawag sa bahagi ng pananalita na naglalarawan sa pangngalan?", "Pandiwa", "Pang-uri", "Pang-abay", "Pangatnig", 1, "Pang-uri ang nagbibigay-katangian sa pangngalan o panghalip."),
            new QuizQuestion("Filipino", "Ano ang tawag sa bahagi ng pananalita na nagsasaad ng kilos o galaw?", "Pangngalan", "Panghalip", "Pandiwa", "Pang-ukol", 2, "Pandiwa ang mga salitang nagpapakita ng kilos o aksyon."),
            new QuizQuestion("Filipino", "Kailan ipinagdiriwang ang Araw ng Kalayaan ng Pilipinas?", "Hunyo 12", "Hulyo 4", "Agosto 21", "Nobyembre 30", 0, "Iprinoklama ang kalayaan sa Kawit, Cavite noong Hunyo 12, 1898."),

            // GENERAL KNOWLEDGE
            new QuizQuestion("General Knowledge", "Which river is the longest river in Africa?", "Congo River", "Nile River", "Zambezi River", "Niger River", 1, "The Nile stretches over 6,650 km across northeastern Africa."),
            new QuizQuestion("General Knowledge", "In which year did humans first land on the Moon?", "1965", "1969", "1972", "1975", 1, "Apollo 11 landed on the Moon on July 20, 1969."),
            new QuizQuestion("General Knowledge", "What is the capital city of Japan?", "Kyoto", "Osaka", "Tokyo", "Yokohama", 2, "Tokyo has served as Japan's capital since 1868."),
            new QuizQuestion("General Knowledge", "What is the deepest known oceanic trench on Earth?", "Java Trench", "Puerto Rico Trench", "Mariana Trench", "Tonga Trench", 2, "Challenger Deep in the Mariana Trench reaches nearly 11 km deep."),
            new QuizQuestion("General Knowledge", "How many bones are in the adult human body?", "186", "206", "226", "256", 1, "An adult human skeleton consists of 206 bones."),
            new QuizQuestion("General Knowledge", "Which is the largest hot desert in the world?", "Gobi Desert", "Kalahari Desert", "Sahara Desert", "Arabian Desert", 2, "The Sahara covers over 9 million sq km across North Africa.")
        };
    }
}
