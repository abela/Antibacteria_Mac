//
//  CoreSettings.m
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/10/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import "CoreSettings.h"

@implementation CoreSettings
//
@synthesize needGaussianBlurLoad;
@synthesize upperLeft;
@synthesize upperRight;
@synthesize bottomLeft;
@synthesize bottomRight;
@synthesize aspectRatio;
@synthesize defaultScreenResolutionWidth;
@synthesize mainWindow;
//
CoreSettings *_sharedCoreSettings = nil;
//
+(CoreSettings*) sharedCoreSettings
{
    @synchronized([CoreSettings class])
    {
        if(!_sharedCoreSettings)
            _sharedCoreSettings = [[CoreSettings alloc] init];
        return _sharedCoreSettings;
    }
}
//
-(id) init
{
    if((self = [super init]))
    {
        needGaussianBlurLoad = NO;
        return self;
    }
    return nil;
}
//
@end
