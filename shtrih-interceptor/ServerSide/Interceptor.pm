package VCS::Interceptor;
use strict;

use VCS::Config;
use LWP;
use HTTP::Request;
use Digest::MD5  qw(md5_hex);
use Data::Dumper;
use Encode qw(decode encode);

sub new
# //////////////////////////////////////////////////
{
	my ( $class, $pclass, $vars ) = @_;
	
	my $self = bless {}, $pclass;
	
	$self->{ 'VCS::Vars' } = $vars;
	
	return $self;
}

	my $serv = '127.0.0.1';
	

sub send_docpack
# //////////////////////////////////////////////////
{

	my ( $self, $docid, $ptype, $summ ) = @_;
	
	my $data = {};
	my $vars = $self->{ 'VCS::Vars' };
	my $docobj = VCS::Docs::docs->new('VCS::Docs::docs', $vars);
	my $error = $docobj->getDocData(\$data, $docid, 'individuals');
	
	return ( "ERROR", "Договор юрлица не может быть оплачен" ) if $data->{ jurid };
	
	my $services = temporary_docservices( $self, $data, $ptype, $summ );

	my $request = xml_create( $services->{ services }, $services->{ info } );
	
	my $resp = send_request( $request );
	
	return split( /:/, $resp );
}

sub xml_create
# //////////////////////////////////////////////////
{
	my ( $services, $info ) = @_;
	
	
	my $md5line = '';
	my $xml = '<Services>';
	
	for my $service ( keys %$services ) {
	
		$xml .= '<Service>';
	
		for my $field ( sort { $a cmp $b } keys %{ $services->{ $service } } ) {
		
			$xml .= "<$field>" . $services->{ $service }->{ $field } . "</$field>";
			
			$md5line .= $services->{ $service }->{ $field };
		}
		
		$xml .= '</Service>';
	}
	
	$xml .= '</Services>';
	
	$xml .= '<Info>';
	
	for ( sort { $a cmp $b } keys %$info ) {
	
		$xml .= "<$_>" . $info->{ $_ } . "</$_>";
		
		$md5line .= $info->{ $_ };
	}
	
	$xml .= '</Info>';
	
	my $currentDate = get_date();
	
	$md5line .= $currentDate;
	
	Encode::from_to( $md5line, 'utf-8', 'windows-1251' );
	
	my $bytecode = "";
	
	$bytecode .= ord($_) . " " for split( //, $md5line );
	
	my $md5 = Digest::MD5->new->add( $bytecode )->hexdigest;
	
	$xml =  '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			$xml .
			'<Control>' .
				'<CRC>' . $md5 . '</CRC>' . 
				'<Date>' . $currentDate . '</Date>' .
			'</Control>' .
		'</toCashbox>';

	return $xml;
}

sub send_request
# //////////////////////////////////////////////////
{
	my $line = shift;
	
	my $ua = LWP::UserAgent->new;
	
	$ua->agent('Mozilla/4.0 (compatible; MSIE 6.0; X11; Linux i686; en) Opera 7.60');
	
	my $request = HTTP::Request->new(GET => $serv.'/?message='.$line.';');

	my $response = $ua->request($request);

	return "ERROR:нет связи с перехватчиком" if $response->{ _rc } != 200;
	
	return $response->{ _content };
}

sub get_date
# //////////////////////////////////////////////////
{
	my ( $sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst ) = localtime( time );
	
	$year += 1900;
	$mon++;
	
	for ( $sec, $min, $hour, $mday, $mon, $year ) { 
	
		$_ = '0'.$_ if $_ < 10;
	};
	
	return "$year-$mon-$mday $hour:$min:$sec";
}

sub get_all_add_services
# //////////////////////////////////////////////////
{
	my ( $self, $vars, $docid, $rate ) = @_;
	
	my $serv_price = $vars->db->selallkeys("
		SELECT ServiceID, Price FROM ServicesPriceRates WHERE PriceRateID = ?", $rate );
		
	my %serv_price = map { $_->{ ServiceID } => $_->{ Price } } @$serv_price;
		
	my $services = $vars->db->selallkeys("
		SELECT Services.Name, DocPackService.ServiceID, ServiceFields.ValueType, ServiceFieldValuesINT.Value
		FROM DocPackService
		JOIN Services ON DocPackService.ServiceID = Services.ID
		JOIN ServiceFields ON DocPackService.ServiceID = ServiceFields.ServiceID
		JOIN ServiceFieldValuesINT ON ServiceFieldValuesINT.DocPackServiceID = DocPackService.ID
		WHERE PackID = ?
		GROUP BY DocPackServiceID",
		$docid
	);
	
	my $serv_list = {};
	
	my $serv_index = 0;
	
	for ( @$services ) {
	
		$serv_index++;
	
		if ( $_->{ ValueType } == 1 ) {
			
			$serv_list->{ "service$serv_index" } = {
				Name		=> $_->{ Name },
				Quantity	=> 1,
				Price		=> $_->{ Value },
				VAT 		=> 1,

			};
		}
		elsif ( $_->{ ValueType } == 2 ) {
		
			$serv_list->{ "service$serv_index" } = {
				Name		=> $_->{ Name },
				Quantity	=> $_->{ Value },
				Price		=> $serv_price{ $_->{ ServiceID } },
				VAT 		=> 1,
			};
		}
	}
	
	return $serv_list;
}


sub temporary_docservices
# //////////////////////////////////////////////////
{
	my ( $self, $data, $ptype, $summ ) = @_;

	my $vars = $self->{'VCS::Vars'};
	my $gconfig = $vars->getConfig('general');

	my $mobile_kostyl = 0;
	my $kostyle_hash = {};

	my ($k_agr, $k_code, $k_name, $k_pers, $k_price, $k_prsum, $k_agrsum, $k_agrvat) = $vars->db->sel1("
		SELECT AgreementNo, Code, Name, PersNum, Price, PriceSum, AgrSum, AgrVAT
		FROM MobilJurDoc WHERE AgreementNo = ?", $data->{docnum} );
	
	if ( $k_agr ) { 
		$kostyle_hash->{code} = $k_code;
		$kostyle_hash->{num} = $k_pers;
		$kostyle_hash->{price} = $k_price;
		$kostyle_hash->{price_sum} = $k_prsum;
		$kostyle_hash->{sum} = $k_agrsum;
		$kostyle_hash->{nds} = $k_agrvat;
		$kostyle_hash->{name} = $k_name;
		
		$mobile_kostyl = 1;
	}
	
	my ( $cntres, $cntnres, $cntncon, $cntage, $smscnt, $shcnt, $shrows, $shind, $indexes, $dhlsum, $inssum, $inscnt ) = 
		( 0, 0, 0, 0, 0, 0, {}, '', {}, 0, 0, 0 );

	if ( $data->{ shipping }==1 ) {
		$dhlsum = $data->{'shipsum'};
		$shcnt = 1;
	}

	my ( $apcnt, $astr, $bankid, $prevbank ) = ( 0, '', '', 0 );
	
	for my $ak ( @{ $data->{ applicants } } ) {

		next if $ak->{ Status } == 7;

		$apcnt++;
		
		if ( $ak->{ Concil } ) {
		
			$cntncon++;
		}
		else {
			if ( $ak->{ AgeCatA } ) {
			
				$cntage++;
			}
			else {
				if ( $ak->{ iNRes } ) {
				
					$cntnres++;
				}
				else {					
					$cntres++;
				}
			}
		}

		if ( ( $data->{ sms_status } == 2 ) && ( $ak->{ MobileNums } ne '' ) ) {
		
			$smscnt++;
		}
		
		if ( ( $data->{ shipping } == 2 ) && ( $ak->{ ShipAddress } ne '' ) ) {
		
			$shcnt++;
			$dhlsum += $ak->{ RTShipSum };
		}
	}
	
	my ( $agesfree, $ages ) = $vars->db->sel1("
		SELECT AgesFree, Ages FROM PriceRate WHERE ID = ?",
		$data->{ rate }
	);
	
	my $prices = $vars->admfunc->getPrices( $vars, $data->{ rate }, $data->{ vtype }, $data->{ ipdate } );

	my $shsum = 0;
	
	if ( $data->{ newdhl } ) {
	
		$shsum = sprintf( "%.2f", $dhlsum );
	}
	else {
		$shsum = sprintf( "%.2f", $prices->{ shipping } * $shcnt );
	}
	
	if ( $data->{ sms_status } == 1 ) {
	
		$smscnt = 1;
	}
	
	if ( $data->{ inssum } ne '0.00' ) {
	
		$inssum = $data->{ inssum };
		$inscnt = 1;
	}
	
	my $vprice = (
		$data->{ urgent } ?
		$prices->{ ( $data->{ jurid } ? 'j' : '' ) . 'urgent' } :
		$prices->{ ( $data->{ jurid } ? 'j' : '') . 'visa' }
	);
	    
	my $servsums = {
		insurance => {
			Name		=> 'Страхование',
			Quantity	=> $inscnt,
			Price		=> $inssum,
			VAT		=> 0,
		},
		shipping => {
			Name		=> 'Услуги по доставке на дом',
			Quantity	=> $shcnt,
			Price		=> sprintf( "%.2f", $shsum ),
			VAT		=> 1,
		},
		sms => {
			Name		=> 'Услуги по СМС оповещению',
			Quantity	=> $smscnt,
			Price		=> sprintf( "%.2f", $prices->{ sms } ),
			VAT		=> 1,
		},
		tran => {
			Name		=> 'Услуги по переводу документов',
			Quantity	=> $data->{ transum },
			Price		=> sprintf( "%.2f", $prices->{ tran } ),
			VAT		=> 1,
		},
		xerox => {
			Name		=> 'Услуги по копированию',
			Quantity	=> $data->{ xerox },
			Price		=> sprintf( "%.2f", $prices->{ xerox } ),
			VAT		=> 1,
		},
		visa => {
			Name		=> ( $data->{'urgent'} ? 'Cрочн.cервисный' : 'Cервисный' ).' сбор',
			Quantity	=> $apcnt,
			Price		=> sprintf( "%.2f", $vprice ),
			VAT		=> 1,
		},
		ank => {
			Name		=> '00502 Услуги по заполнению анкеты',
			Quantity	=> $data->{ anketasrv },
			Price		=> sprintf( "%.2f", $prices->{ anketasrv } ),
			VAT		=> 1,
		},
		print => {
			Name		=> '00503 Услуги по распечатке',
			Quantity	=> $data->{ printsrv },
			Price		=> sprintf( "%.2f", $prices->{ printsrv } ),
			VAT		=> 1,
		},
		photo => {
			Name		=> '00504 Услуги по фотографированию',
			Quantity	=> $data->{ photosrv },
			Price		=> sprintf( "%.2f", $prices->{ photosrv } ),
			VAT		=> 1,
		},
		vip => {
			Name		=> 'ВИП обслуживание',
			Quantity	=> $data->{ vipsrv },
			Price		=> sprintf( "%.2f", $prices->{ vipsrv } ),
			VAT		=> 1,
		},
		cons_resident => {
			Name		=> 'Консульский сбор (резидент)',
			Quantity	=> ( $data->{'vcat'} eq 'C' ? $cntres : 0 ),
			Price		=> sprintf( '%.2f', $prices->{ 'concilr' . ( $data->{ urgent } ? 'u' : '' ) } ),
			VAT		=> 0,
		},
		cons_noresident => {
			Name		=> 'Консульский сбор (нерезидент)',
			Quantity	=> $cntnres,
			Price		=> sprintf( '%.2f', $prices->{ 'conciln' . ( $data->{ urgent } ? 'u' : '' ) } ),
			VAT		=> 0,
		},
		cons_age => {
			Name		=> 'Консульский сбор (возраст)',
			Quantity	=> $cntage,
			Price		=> sprintf( '%.2f', $prices->{ 'concilr' . ( $data->{ urgent } ? 'u' : '' ) . '_' . $ages } ),
			VAT		=> 0,
		},
		cons_d => {
			Name		=> 'Консульский сбор (тип D)',
			Quantity	=> ( $data->{'vcat'} eq 'D' ? $cntres : 0 ),
			Price		=> sprintf( '%.2f', $prices->{ 'concilr' . ( $data->{ urgent } ? 'u' : '' ) } ),
			VAT		=> 0,
		},
	};

	my $serv_hash = get_all_add_services( $self, $vars, $data->{ docid }, $data->{'rate'} );
	
	for ( keys %$serv_hash ) {
	
		$servsums->{ $_ } = $serv_hash->{ $_ };
	}
	
	if ( $mobile_kostyl ) {
	
		$servsums->{'visaprc'} = $kostyle_hash->{price};
		$servsums->{'visasum'} = $kostyle_hash->{price_sum};
	}

	for my $serv ( keys %$servsums ) {
	
		delete $servsums->{ $serv } unless $servsums->{ $serv }->{ Quantity };
	}
	
	my $info = {
		AgrNumber => $data->{ docnum },
		Cashier => 'Иванов И.И.',
		CashierPass => 26,
		
		MoneyType => $ptype,
		Total => '169.00',
		Money => $summ,
	};
	
	return { services => $servsums, info => $info };
}

1;
