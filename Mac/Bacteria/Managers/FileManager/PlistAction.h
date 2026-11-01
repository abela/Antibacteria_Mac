//
//  PlistAction.h
//  PlistActions
//
//  Created by Giorgi Abelashvili on 1/8/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>


@interface PlistAction : NSObject {

}

+ (NSString*)dataFilePath:(NSString*)name;


+ (void)writeStringInPlist:(NSString*)str atFileName:(NSString*)fileName;


+ (NSString*)readStringFromPlist:(int)index atFileNamePath:(NSString*)path;


+(void)addArrayToPlist:(NSMutableArray*)array atPath:(NSString*)path;
@end
