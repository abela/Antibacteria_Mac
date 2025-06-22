//
//  LocalizationManager.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 8/1/13.
//
//
//
#import <Foundation/Foundation.h>
//
//
@interface LocalizationManager : NSObject
{
    NSArray *localizedData;
    Localizations currentLocalization;
}
//
@property (nonatomic,assign) Localizations currentLocalization;
//
+(LocalizationManager*) sharedLocalizationManager;
-(NSString*) getValueWithKey:(NSString*)key;
-(void) saveCurrentLocalization;
@end
