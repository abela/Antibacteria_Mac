//
//  LocalizationManager.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 8/1/13.
//
//
//
#import "LocalizationManager.h"
#import "NSData+AES256.h"
//
#define CURRENT_LOCALIZATION        @"currentlocalization"
//
LocalizationManager* _sharedLocalizationManager;
//
@interface LocalizationManager (PrivateMethods)
-(void) readData;
-(void) getCurrentLocalization;
@end
//
@implementation LocalizationManager
//
@synthesize currentLocalization;
//
+(LocalizationManager*) sharedLocalizationManager
{
    @synchronized([LocalizationManager class])
    {
        if (!_sharedLocalizationManager) {
            _sharedLocalizationManager = [[LocalizationManager alloc] init];
        }
        return _sharedLocalizationManager;
    }
}
//
-(id) init
{
    if ((self = [super init])) {
        // read data at start
        [self readData];
        //
        return self;
    }
    return nil;
}
//
-(void) readData
{
    NSString *path = [[NSBundle mainBundle] pathForResource:@"localizations" ofType:@"plist"];
	localizedData = [[NSArray arrayWithContentsOfFile:path] retain];
    // get current localization
    [self getCurrentLocalization];
}
//
-(NSString*) getValueWithKey:(NSString*)key
{
    NSString *str = @"";
    int index = ((int)currentLocalization);
    NSDictionary *languageDictionary = [localizedData objectAtIndex:index];
    str = [languageDictionary objectForKey:key];
    return str;
}
//
-(void) getCurrentLocalization
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSData *localizedDataCrypt = [defaults objectForKey:CURRENT_LOCALIZATION];
    if (localizedDataCrypt == nil) {
        currentLocalization = kEnglishLocalization;
        [self saveCurrentLocalization];
    }
    else currentLocalization = (Localizations)[[NSKeyedUnarchiver unarchiveObjectWithData:[localizedDataCrypt AES256Decrypt]]
                                               intValue];
}
//
-(void) saveCurrentLocalization
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSData *localizedDataCrypt = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:((int)currentLocalization)]]
                             AES256Encrypt];
    [defaults setObject:localizedDataCrypt forKey:CURRENT_LOCALIZATION];
    [defaults synchronize];
}
//
@end
