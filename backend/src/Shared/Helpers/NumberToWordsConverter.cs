namespace Shared.Helpers;

public static class NumberToWordsConverter
{
    private static readonly string[] Units = { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
    private static readonly string[] Tens = { "", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi" };
    private static readonly string[] Hundreds = { "", "một trăm", "hai trăm", "ba trăm", "bốn trăm", "năm trăm", "sáu trăm", "bảy trăm", "tám trăm", "chín trăm" };

    public static string Convert(decimal number)
    {
        if (number == 0) return "không đồng";
        
        var integerPart = (long)number;
        var decimalPart = (long)((number - integerPart) * 100);
        
        var result = ConvertInteger(integerPart);
        
        if (decimalPart > 0)
        {
            result += " phẩy " + ConvertInteger(decimalPart) + " xu";
        }
        else
        {
            result += " đồng";
        }
        
        return result.Trim();
    }

    private static string ConvertInteger(long number)
    {
        if (number == 0) return "không";
        
        if (number < 10)
            return Units[number];
        
        if (number < 100)
        {
            var tens = number / 10;
            var units = number % 10;
            
            if (tens == 1)
            {
                return units == 5 ? "mười lăm" : units == 0 ? "mười" : $"mười {Units[units]}";
            }
            
            return units == 0 
                ? Tens[tens] 
                : units == 5 && tens > 1 
                    ? $"{Tens[tens]} lăm" 
                    : $"{Tens[tens]} {Units[units]}";
        }
        
        if (number < 1000)
        {
            var hundreds = number / 100;
            var remainder = number % 100;
            
            if (remainder == 0)
                return Hundreds[hundreds];
            
            return $"{Hundreds[hundreds]} {ConvertInteger(remainder)}";
        }
        
        if (number < 1000000)
        {
            var thousands = number / 1000;
            var remainder = number % 1000;
            
            var thousandText = thousands == 1 ? "một nghìn" : $"{ConvertInteger(thousands)} nghìn";
            
            if (remainder == 0)
                return thousandText;
            
            if (remainder < 100)
                return $"{thousandText} lẻ {ConvertInteger(remainder)}";
            
            return $"{thousandText} {ConvertInteger(remainder)}";
        }
        
        if (number < 1000000000)
        {
            var millions = number / 1000000;
            var remainder = number % 1000000;
            
            var millionText = millions == 1 ? "một triệu" : $"{ConvertInteger(millions)} triệu";
            
            if (remainder == 0)
                return millionText;
            
            return $"{millionText} {ConvertInteger(remainder)}";
        }
        
        var billions = number / 1000000000;
        var remainder2 = number % 1000000000;
        
        var billionText = billions == 1 ? "một tỷ" : $"{ConvertInteger(billions)} tỷ";
        
        if (remainder2 == 0)
            return billionText;
        
        return $"{billionText} {ConvertInteger(remainder2)}";
    }
}

